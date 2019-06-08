using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQLParser.AST.OperationType;

namespace GraphQL.Utilities
{
    public class SchemaBuilder
    {
        private readonly IDictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();
        private readonly List<IVisitorSelector> _visitorSelectors = new List<IVisitorSelector>();

        public IServiceProvider ServiceProvider { get; set; } = new DefaultServiceProvider();

        [Obsolete("Use ServiceProvider instead")]
        public IServiceProvider DependencyResolver
        {
            get => ServiceProvider;
            set => ServiceProvider = value;
        }

        public TypeSettings Types { get; } = new TypeSettings();

        public IDictionary<string, Type> Directives { get; } = new Dictionary<string, Type>
        {
            { "deprecated", typeof(DeprecatedDirectiveVisistor) }
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

        public ISchema Build(string[] typeDefinitions)
        {
            return Build(string.Join(Environment.NewLine, typeDefinitions));
        }

        public ISchema Build(string typeDefinitions)
        {
            var document = Parse(typeDefinitions);
            return BuildSchemaFrom(document);
        }

        private static GraphQLDocument Parse(string document)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            return parser.Parse(new Source(document));
        }

        private ISchema BuildSchemaFrom(GraphQLDocument document)
        {
            if (Directives.Any())
            {
                _visitorSelectors.Add(new DirectiveVisitorSelector(Directives, t => (SchemaDirectiveVisitor)ServiceProvider.GetRequiredService(t)));
            }

            var schema = new Schema(ServiceProvider);

            var directives = new List<DirectiveGraphType>();

            GraphQLSchemaDefinition schemaDef = null;

            foreach (var def in document.Definitions)
            {
                switch (def.Kind)
                {
                    case ASTNodeKind.SchemaDefinition:
                    {
                        schemaDef = def as GraphQLSchemaDefinition;
                        break;
                    }

                    case ASTNodeKind.ObjectTypeDefinition:
                    {
                        var type = ToObjectGraphType(def as GraphQLObjectTypeDefinition);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.TypeExtensionDefinition:
                    {
                        var type = ToObjectGraphType((def as GraphQLTypeExtensionDefinition).Definition, true);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InterfaceTypeDefinition:
                    {
                        var type = ToInterfaceType(def as GraphQLInterfaceTypeDefinition);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.EnumTypeDefinition:
                    {
                        var type = ToEnumerationType(def as GraphQLEnumTypeDefinition);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.UnionTypeDefinition:
                    {
                        var type = ToUnionType(def as GraphQLUnionTypeDefinition);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InputObjectTypeDefinition:
                    {
                        var type = ToInputObjectType(def as GraphQLInputObjectTypeDefinition);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.DirectiveDefinition:
                    {
                        var directive = ToDirective(def as GraphQLDirectiveDefinition);
                        directives.Add(directive);
                        break;
                    }
                }
            }

            if (schemaDef != null)
            {
                foreach (var operationTypeDef in schemaDef.OperationTypes)
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

        protected virtual IGraphType GetType(string name)
        {
            _types.TryGetValue(name, out IGraphType type);
            return type;
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
                type = _types[astType.Name.Value] as ObjectGraphType;
            }

            if (!isExtensionType)
            {
                type.Description = typeConfig.Description ?? astType.Comment?.Text;
                type.IsTypeOf = typeConfig.IsTypeOfFunc;
            }

            CopyMetadata(type, typeConfig);

            Func<string, GraphQLFieldDefinition, FieldType> constructFieldType;
            if (type.Name == "Subscription")
            {
                constructFieldType = ToSubscriptionFieldType;
            }
            else
            {
                constructFieldType = ToFieldType;
            }

            var fields = astType.Fields.Select(f => constructFieldType(type.Name, f));
            fields.Apply(f => type.AddField(f));

            var interfaces = astType
                .Interfaces
                .Select(i => new GraphQLTypeReference(i.Name.Value))
                .ToList();
            interfaces.Apply(type.AddResolvedInterface);

            type.SetAstType(astType);

            VisitNode(type, v => v.VisitObjectGraphType(type));

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

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);
            field.DeprecationReason = fieldConfig.DeprecationReason;

            field.SetAstType(fieldDef);

            VisitNode(field, v => v.VisitField(field));

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

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);

            field.SetAstType(fieldDef);

            VisitNode(field, v => v.VisitField(field));

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
            };

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
            };

            CopyMetadata(type, typeConfig);

            var fields = interfaceDef.Fields.Select(f => ToFieldType(type.Name, f));
            fields.Apply(f => type.AddField(f));

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
            };

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
            };

            CopyMetadata(type, typeConfig);

            var fields = inputDef.Fields.Select(x => ToFieldType(type.Name, x));
            fields.Apply(f => type.AddField(f));

            return type;
        }

        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var typeConfig = Types.For(enumDef.Name.Value);

            var type = new EnumerationGraphType
            {
                Name = enumDef.Name.Value,
                Description = typeConfig.Description ?? enumDef.Comment?.Text
            };

            var values = enumDef.Values.Select(ToEnumValue);
            values.Apply(type.AddValue);
            return type;
        }

        protected virtual DirectiveGraphType ToDirective(GraphQLDirectiveDefinition directiveDef)
        {
            var locations = directiveDef.Locations.Select(l => ToDirectiveLocation(l.Value));
            var directive = new DirectiveGraphType(directiveDef.Name.Value, locations)
            {
                Description = directiveDef.Comment?.Text
            };

            var arguments = directiveDef.Arguments.Select(ToArguments);
            directive.Arguments = new QueryArguments(arguments);

            return directive;
        }

        private DirectiveLocation ToDirectiveLocation(string name)
        {
            var enums = new __DirectiveLocation();
            var result = enums.ParseValue(name);
            if (result != null)
            {
                return (DirectiveLocation)result;
            }

            throw new ExecutionError($"{name} is an unknown directive location");
        }

        private EnumValueDefinition ToEnumValue(GraphQLEnumValueDefinition valDef)
        {
            var val = new EnumValueDefinition
            {
                Value = valDef.Name.Value,
                Name = valDef.Name.Value,
                Description = valDef.Comment?.Text
            };

            val.SetAstType(valDef);

            VisitNode(val, v => v.VisitEnumValue(val));

            return val;
        }

        protected virtual QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            return new QueryArgument(type)
            {
                Name = inputDef.Name.Value,
                DefaultValue = inputDef.DefaultValue.ToValue(),
                ResolvedType = ToGraphType(inputDef.Type),
                Description = inputDef.Comment?.Text
            };
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
            foreach(var selector in _visitorSelectors)
            {
                foreach(var visitor in selector.Select(node))
                {
                    action(visitor);
                }
            }
        }
    }

    internal static class SchemaExtensions
    {
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

                    throw new ExecutionError($"Invalid number {str.Value}");
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
                    obj.Fields.Apply(f => values[f.Name.Value] = ToValue(f.Value));
                    return values;
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    Debug.Assert(list != null, nameof(list) + " != null");
                    var values = list.Values.Select(ToValue).ToArray();
                    return values;
                }
                default:
                    throw new ExecutionError($"Unsupported value type {source.Kind}");
            }
        }
    }
}
