using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQLParser.AST.OperationType;

namespace GraphQL.Utilities
{
    public class SchemaBuilder
    {
        protected readonly IDictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();
        private readonly List<IVisitorSelector> _visitorSelectors = new List<IVisitorSelector>();
        private GraphQLSchemaDefinition _schemaDef;

        public IServiceProvider ServiceProvider { get; set; } = new DefaultServiceProvider();

        public TypeSettings Types { get; } = new TypeSettings();

        public IDictionary<string, Type> Directives { get; } = new Dictionary<string, Type>
        {
            { "deprecated", typeof(DeprecatedDirectiveVisitor) }
        };

        public SchemaBuilder RegisterDirectiveVisitor<T>(string name) where T : SchemaDirectiveVisitor
        {
            Directives[name] = typeof(T);
            return this;
        }

        public SchemaBuilder RegisterVisitorSelector<T>(T selector) where T : IVisitorSelector
        {
            _visitorSelectors.Add(selector);
            return this;
        }

        public SchemaBuilder RegisterType(IGraphType type)
        {
            _types[type.Name] = type;
            return this;
        }

        public void RegisterTypes(IEnumerable<IGraphType> types)
        {
            types.Apply(t => _types[t.Name] = t);
        }

        public virtual ISchema Build(string[] typeDefinitions)
        {
            return Build(string.Join(Environment.NewLine, typeDefinitions));
        }

        public virtual ISchema Build(string typeDefinitions)
        {
            var document = Parse(typeDefinitions);
            Validate(document);
            return BuildSchemaFrom(document);
        }

        protected virtual void Validate(GraphQLDocument document)
        {
            var definitionsByName = document.Definitions.OfType<GraphQLTypeDefinition>().Where(def => !(def is GraphQLTypeExtensionDefinition)).ToLookup(def => def.Name.Value);
            var duplicates = definitionsByName.Where(grouping => grouping.Count() > 1).ToArray();
            if (duplicates.Length > 0)
                throw new ArgumentException(@$"All types within a GraphQL schema must have unique names. No two provided types may have the same name.
Schema contains a redefinition of these types: {string.Join(", ", duplicates.Select(item => item.Key))}", nameof(document));

            // checks for parsed SDL may be expanded in the future, see https://github.com/graphql/graphql-spec/issues/653
        }

        private static GraphQLDocument Parse(string document)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            return parser.Parse(new Source(document));
        }

        private ISchema BuildSchemaFrom(GraphQLDocument document)
        {
            if (Directives.Count > 0)
            {
                _visitorSelectors.Add(new DirectiveVisitorSelector(Directives, t => (SchemaDirectiveVisitor)ServiceProvider.GetRequiredService(t)));
            }

            var schema = new Schema(ServiceProvider);

            PreConfigure(schema);

            var directives = new List<DirectiveGraphType>();

            foreach (var def in document.Definitions)
            {
                switch (def.Kind)
                {
                    case ASTNodeKind.SchemaDefinition:
                    {
                        _schemaDef = def.As<GraphQLSchemaDefinition>();
                        schema.SetAstType(_schemaDef);

                        VisitNode(schema, v => v.VisitSchema(schema));
                        break;
                    }

                    case ASTNodeKind.ObjectTypeDefinition:
                    {
                        var type = ToObjectGraphType(def.As<GraphQLObjectTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.TypeExtensionDefinition:
                    {
                        var type = ToObjectGraphType(def.As<GraphQLTypeExtensionDefinition>().Definition, true);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InterfaceTypeDefinition:
                    {
                        var type = ToInterfaceType(def.As<GraphQLInterfaceTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.EnumTypeDefinition:
                    {
                        var type = ToEnumerationType(def.As<GraphQLEnumTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.UnionTypeDefinition:
                    {
                        var type = ToUnionType(def.As<GraphQLUnionTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InputObjectTypeDefinition:
                    {
                        var type = ToInputObjectType(def.As<GraphQLInputObjectTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.DirectiveDefinition:
                    {
                        var directive = ToDirective(def.As<GraphQLDirectiveDefinition>());
                        directives.Add(directive);
                        break;
                    }
                }
            }

            if (_schemaDef != null)
            {
                schema.Description = _schemaDef.Comment?.Text;

                foreach (var operationTypeDef in _schemaDef.OperationTypes)
                {
                    var typeName = operationTypeDef.Type.Name.Value;
                    var type = GetType(typeName) as IObjectGraphType;

                    switch (operationTypeDef.Operation)
                    {
                        case OperationType.Query:
                            schema.Query = type;
                            break;

                        case OperationType.Mutation:
                            schema.Mutation = type;
                            break;

                        case OperationType.Subscription:
                            schema.Subscription = type;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Unknown operation type {operationTypeDef.Operation}");
                    }
                }
            }
            else
            {
                schema.Query = GetType("Query") as IObjectGraphType;
                schema.Mutation = GetType("Mutation") as IObjectGraphType;
                schema.Subscription = GetType("Subscription") as IObjectGraphType;
            }

            var typeList = _types.Values.ToArray();
            typeList.Apply(schema.RegisterType);
            schema.RegisterDirectives(directives);

            return schema;
        }

        protected virtual void PreConfigure(Schema schema)
        {
        }

        protected virtual IGraphType GetType(string name)
        {
            _types.TryGetValue(name, out IGraphType type);
            return type;
        }

        private bool IsSubscriptionType(ObjectGraphType type)
        {
            var operationDefinition = _schemaDef?.OperationTypes?.FirstOrDefault(o => o.Operation == OperationType.Subscription);
            if (operationDefinition == null)
                return type.Name == "Subscription";

            return type.Name == operationDefinition.Type.Name.Value;
        }

        protected virtual IObjectGraphType ToObjectGraphType(GraphQLObjectTypeDefinition astType, bool isExtensionType = false)
        {
            var typeConfig = Types.For(astType.Name.Value);

            ObjectGraphType type;
            if (!_types.ContainsKey(astType.Name.Value))
            {
                type = new ObjectGraphType { Name = astType.Name.Value };
            }
            else
            {
                type = _types[astType.Name.Value] as ObjectGraphType ?? throw new InvalidOperationException($"Type '{astType.Name.Value} should be ObjectGraphType");
            }

            if (!isExtensionType)
            {
                type.Description = typeConfig.Description ?? astType.Comment?.Text;
                type.IsTypeOf = typeConfig.IsTypeOfFunc;
            }

            CopyMetadata(type, typeConfig);

            Func<string, GraphQLFieldDefinition, FieldType> constructFieldType;
            if (IsSubscriptionType(type))
            {
                constructFieldType = ToSubscriptionFieldType;
            }
            else
            {
                constructFieldType = ToFieldType;
            }

            if (astType.Fields != null)
            {
                var fields = astType.Fields.Select(f => constructFieldType(type.Name, f));
                fields.Apply(f => type.AddField(f));
            }

            astType.Interfaces?.Select(i => new GraphQLTypeReference(i.Name.Value))
                .Apply(type.AddResolvedInterface);

            if (isExtensionType)
            {
                type.AddExtensionAstType(astType);
            }
            else
            {
                type.SetAstType(astType);
            }

            VisitNode(type, v => v.VisitObject(type));

            return type;
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);
            var fieldConfig = typeConfig.FieldFor(fieldDef.Name.Value, ServiceProvider);

            var field = new FieldType
            {
                Name = fieldDef.Name.Value,
                Description = fieldConfig.Description ?? fieldDef.Comment?.Text,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver
            };

            CopyMetadata(field, fieldConfig);

            field.Arguments = ToQueryArguments(fieldDef.Arguments);
            field.DeprecationReason = fieldConfig.DeprecationReason;

            field.SetAstType(fieldDef);

            VisitNode(field, v => v.VisitFieldDefinition(field));

            return field;
        }

        protected virtual FieldType ToSubscriptionFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);
            var fieldConfig = typeConfig.SubscriptionFieldFor(fieldDef.Name.Value, ServiceProvider);

            var field = new EventStreamFieldType
            {
                Name = fieldDef.Name.Value,
                Description = fieldConfig.Description ?? fieldDef.Comment?.Text,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver,
                Subscriber = fieldConfig.Subscriber,
                AsyncSubscriber = fieldConfig.AsyncSubscriber,
                DeprecationReason = fieldConfig.DeprecationReason
            };

            CopyMetadata(field, fieldConfig);

            field.Arguments = ToQueryArguments(fieldDef.Arguments);

            field.SetAstType(fieldDef);

            VisitNode(field, v => v.VisitFieldDefinition(field));

            return field;
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLInputValueDefinition inputDef)
        {
            var typeConfig = Types.For(parentTypeName);
            var fieldConfig = typeConfig.FieldFor(inputDef.Name.Value, ServiceProvider);

            var field = new FieldType
            {
                Name = inputDef.Name.Value,
                Description = fieldConfig.Description ?? inputDef.Comment?.Text,
                ResolvedType = ToGraphType(inputDef.Type),
                DefaultValue = inputDef.DefaultValue.ToValue()
            }.SetAstType(inputDef);

            VisitNode(field, v => v.VisitInputFieldDefinition(field));

            return field;
        }

        protected virtual InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var typeConfig = Types.For(interfaceDef.Name.Value);

            var type = new InterfaceGraphType
            {
                Name = interfaceDef.Name.Value,
                Description = typeConfig.Description ?? interfaceDef.Comment?.Text,
                ResolveType = typeConfig.ResolveType
            }.SetAstType(interfaceDef);

            VisitNode(type, v => v.VisitInterface(type));

            CopyMetadata(type, typeConfig);

            if (interfaceDef.Fields != null)
            {
                var fields = interfaceDef.Fields.Select(f => ToFieldType(type.Name, f));
                fields.Apply(f => type.AddField(f));
            }

            return type;
        }

        protected virtual UnionGraphType ToUnionType(GraphQLUnionTypeDefinition unionDef)
        {
            var typeConfig = Types.For(unionDef.Name.Value);

            var type = new UnionGraphType
            {
                Name = unionDef.Name.Value,
                Description = typeConfig.Description ?? unionDef.Comment?.Text,
                ResolveType = typeConfig.ResolveType
            }.SetAstType(unionDef);

            VisitNode(type, v => v.VisitUnion(type));

            CopyMetadata(type, typeConfig);

            var possibleTypes = unionDef.Types.Select(x => GetType(x.Name.Value) ?? new GraphQLTypeReference(x.Name.Value));
            possibleTypes.Apply(x => type.AddPossibleType(x as IObjectGraphType));
            return type;
        }

        protected virtual InputObjectGraphType ToInputObjectType(GraphQLInputObjectTypeDefinition inputDef)
        {
            var typeConfig = Types.For(inputDef.Name.Value);

            var type = new InputObjectGraphType
            {
                Name = inputDef.Name.Value,
                Description = typeConfig.Description ?? inputDef.Comment?.Text
            }.SetAstType(inputDef);

            VisitNode(type, v => v.VisitInputObject(type));

            CopyMetadata(type, typeConfig);

            if (inputDef.Fields != null)
            {
                var fields = inputDef.Fields.Select(x => ToFieldType(type.Name, x));
                fields.Apply(f => type.AddField(f));
            }

            return type;
        }

        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var typeConfig = Types.For(enumDef.Name.Value);

            var type = new EnumerationGraphType
            {
                Name = enumDef.Name.Value,
                Description = typeConfig.Description ?? enumDef.Comment?.Text
            }.SetAstType(enumDef);

            VisitNode(type, v => v.VisitEnum(type));

            var values = enumDef.Values.Select(ToEnumValue);
            values.Apply(type.AddValue);
            return type;
        }

        protected virtual DirectiveGraphType ToDirective(GraphQLDirectiveDefinition directiveDef)
        {
            var locations = directiveDef.Locations.Select(l => ToDirectiveLocation(l.Value));
            return new DirectiveGraphType(directiveDef.Name.Value, locations)
            {
                Description = directiveDef.Comment?.Text,
                Arguments = ToQueryArguments(directiveDef.Arguments)
            };
        }

        private DirectiveLocation ToDirectiveLocation(string name)
        {
            var enums = new __DirectiveLocation();
            var result = enums.ParseValue(name);
            if (result != null)
            {
                return (DirectiveLocation)result;
            }

            throw new ArgumentOutOfRangeException(nameof(name), $"{name} is an unknown directive location");
        }

        private EnumValueDefinition ToEnumValue(GraphQLEnumValueDefinition valDef)
        {
            var val = new EnumValueDefinition
            {
                Value = valDef.Name.Value,
                Name = valDef.Name.Value,
                Description = valDef.Comment?.Text
            }.SetAstType(valDef);

            VisitNode(val, v => v.VisitEnumValue(val));

            return val;
        }

        protected virtual QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            var argument = new QueryArgument(type)
            {
                Name = inputDef.Name.Value,
                DefaultValue = inputDef.DefaultValue.ToValue(),
                ResolvedType = ToGraphType(inputDef.Type),
                Description = inputDef.Comment?.Text
            }.SetAstType(inputDef);

            VisitNode(argument, v => v.VisitArgumentDefinition(argument));

            return argument;
        }

        private IGraphType ToGraphType(GraphQLType astType)
        {
            switch (astType.Kind)
            {
                case ASTNodeKind.NonNullType:
                {
                    var type = ToGraphType(((GraphQLNonNullType)astType).Type);
                    return new NonNullGraphType(type);
                }

                case ASTNodeKind.ListType:
                {
                    var type = ToGraphType(((GraphQLListType)astType).Type);
                    return new ListGraphType(type);
                }

                case ASTNodeKind.NamedType:
                {
                    var namedType = (GraphQLNamedType)astType;
                    var type = GetType(namedType.Name.Value);
                    return type ?? new GraphQLTypeReference(namedType.Name.Value);
                }

                default:
                    throw new ArgumentOutOfRangeException($"Unknown GraphQL type {astType.Kind}");
            }
        }

        protected virtual void CopyMetadata(IProvideMetadata target, IProvideMetadata source)
        {
            source.Metadata.Apply(kv => target.Metadata[kv.Key] = kv.Value);
        }

        protected virtual void VisitNode(object node, Action<ISchemaNodeVisitor> action)
        {
            foreach (var selector in _visitorSelectors)
            {
                foreach (var visitor in selector.Select(node))
                {
                    action(visitor);
                }
            }
        }

        private QueryArguments ToQueryArguments(List<GraphQLInputValueDefinition> arguments)
        {
            return arguments == null ? new QueryArguments() : new QueryArguments(arguments.Select(ToArguments));
        }
    }

    internal static class SchemaExtensions
    {
        public static TNode As<TNode>(ASTNode node) where TNode : ASTNode
        {
            return node as TNode ?? throw new InvalidOperationException($"Node should be of type '{typeof(TNode).Name}' but it is of type '{node?.GetType().Name}'.");
        }

        public static GraphQLDirective Directive(this IEnumerable<GraphQLDirective> directives, string name)
        {
            return directives?.FirstOrDefault(x => string.Equals(x.Name.Value, name, StringComparison.OrdinalIgnoreCase));
        }

        public static GraphQLArgument Argument(this IEnumerable<GraphQLArgument> arguments, string name)
        {
            return arguments?.FirstOrDefault(x => string.Equals(x.Name.Value, name, StringComparison.OrdinalIgnoreCase));
        }

        public static object ToValue(this GraphQLValue source)
        {
            if (source == null)
            {
                return null;
            }

            switch (source.Kind)
            {
                case ASTNodeKind.NullValue:
                {
                    return null;
                }
                case ASTNodeKind.StringValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return str.Value;
                }
                case ASTNodeKind.IntValue:
                {
                    var str = source as GraphQLScalarValue;

                    Debug.Assert(str != null, nameof(str) + " != null");
                    if (int.TryParse(str.Value, out var intResult))
                    {
                        return intResult;
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (long.TryParse(str.Value, out var longResult))
                    {
                        return longResult;
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (decimal.TryParse(str.Value, out var decimalResult))
                    {
                        return decimalResult;
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInteger.TryParse(str.Value, out var bigIntegerResult))
                    {
                        return bigIntegerResult;
                    }

                    throw new InvalidOperationException($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return ValueConverter.ConvertTo<double>(str.Value);
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return ValueConverter.ConvertTo<bool>(str.Value);
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return str.Value;
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = source as GraphQLObjectValue;
                    var values = new Dictionary<string, object>();

                    Debug.Assert(obj != null, nameof(obj) + " != null");
                    obj.Fields?.Apply(f => values[f.Name.Value] = ToValue(f.Value));
                    return values;
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    Debug.Assert(list != null, nameof(list) + " != null");

                    if (list.Values == null)
                        return Array.Empty<object>();

                    object[] values = list.Values.Select(ToValue).ToArray();
                    return values;
                }
                default:
                    throw new InvalidOperationException($"Unsupported value type {source.Kind}");
            }
        }
    }
}
