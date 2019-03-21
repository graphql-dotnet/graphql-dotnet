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

        public IDependencyResolver DependencyResolver { get; set; } = new DefaultDependencyResolver();

        public TypeSettings Types { get; } = new TypeSettings();

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
            var ast = parser.Parse(new Source(document));
            return ast;
        }

        private ISchema BuildSchemaFrom(GraphQLDocument document)
        {
            var schema = new Schema(DependencyResolver);

            var directives = new List<DirectiveGraphType>();

            GraphQLSchemaDefinition schemaDef = null;

            foreach (var def in document.Definitions)
            {
                switch (def.Kind)
                {
                    case ASTNodeKind.SchemaDefinition:
                        schemaDef = def as GraphQLSchemaDefinition;
                        break;

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
                foreach (var op in schemaDef.OperationTypes)
                {
                    var typeName = op.Type.Name.Value;
                    var type = GetType(typeName) as IObjectGraphType;

                    switch (op.Operation)
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
                            throw new ArgumentOutOfRangeException($"Unknown operation type {op.Operation}");
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
            schema.RegisterDirectives(directives.ToArray());

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
                type = new ObjectGraphType {Name = astType.Name.Value};
            }
            else
            {
                type = _types[astType.Name.Value] as ObjectGraphType;
            }

            if (!isExtensionType)
            {
                type.Description = typeConfig.Description;
                type.IsTypeOf = typeConfig.IsTypeOfFunc;

                ApplyDeprecatedDirective(astType.Directives, reason =>
                {
                    type.DeprecationReason = typeConfig.DeprecationReason ?? reason;
                });
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
            fields.Apply(f =>
            {
                type.AddField(f);
            });

            var interfaces = astType
                .Interfaces
                .Select(i => new GraphQLTypeReference(i.Name.Value))
                .ToList();
            interfaces.Apply(type.AddResolvedInterface);

            return type;
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);
            var fieldConfig = typeConfig.FieldFor(fieldDef.Name.Value, DependencyResolver);

            var field = new FieldType
            {
                Name = fieldDef.Name.Value,
                Description = fieldConfig.Description,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver
            };

            CopyMetadata(field, fieldConfig);

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);

            ApplyDeprecatedDirective(fieldDef.Directives, reason =>
            {
                field.DeprecationReason = fieldConfig.DeprecationReason ?? reason;
            });

            return field;
        }

        protected virtual FieldType ToSubscriptionFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);
            var fieldConfig = typeConfig.SubscriptionFieldFor(fieldDef.Name.Value, DependencyResolver);

            var field = new EventStreamFieldType
            {
                Name = fieldDef.Name.Value,
                Description = fieldConfig.Description,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver,
                Subscriber = fieldConfig.Subscriber,
                AsyncSubscriber = fieldConfig.AsyncSubscriber
            };

            CopyMetadata(field, fieldConfig);

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);

            ApplyDeprecatedDirective(fieldDef.Directives, reason =>
            {
                field.DeprecationReason = fieldConfig.DeprecationReason ?? reason;
            });

            return field;
        }

        private static readonly string DeprecatedDefaultValue = DirectiveGraphType.Deprecated.Arguments.Find("reason").DefaultValue.ToString();
        private void ApplyDeprecatedDirective(IEnumerable<GraphQLDirective> directives, Action<string> apply)
        {
            var deprecated = directives.Directive("deprecated");

            if (deprecated != null)
            {
                var arg = deprecated.Arguments.Argument("reason");
                var value = "";

                if (arg != null)
                {
                    value = ToValue(arg.Value).ToString();
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    value = DeprecatedDefaultValue;
                }

                apply(value);
            }
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLInputValueDefinition inputDef)
        {
            var field = new FieldType
            {
                Name = inputDef.Name.Value,
                ResolvedType = ToGraphType(inputDef.Type),
                DefaultValue = ToValue(inputDef.DefaultValue)
            };

            ApplyDeprecatedDirective(inputDef.Directives, reason =>
            {
                field.DeprecationReason = reason;
            });

            return field;
        }

        protected virtual InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var typeConfig = Types.For(interfaceDef.Name.Value);

            var type = new InterfaceGraphType
            {
                Name = interfaceDef.Name.Value,
                Description = typeConfig.Description,
                ResolveType = typeConfig.ResolveType
            };

            ApplyDeprecatedDirective(interfaceDef.Directives, reason =>
            {
                type.DeprecationReason = typeConfig.DeprecationReason ?? reason;
            });

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
                Description = typeConfig.Description,
                ResolveType = typeConfig.ResolveType
            };

            ApplyDeprecatedDirective(unionDef.Directives, reason =>
            {
                type.DeprecationReason = typeConfig.DeprecationReason ?? reason;
            });

            CopyMetadata(type, typeConfig);

            var possibleTypes = unionDef.Types.Select(x => GetType(x.Name.Value) ?? new GraphQLTypeReference(x.Name.Value));
            possibleTypes.Apply(x => type.AddPossibleType(x as IObjectGraphType));
            return type;
        }

        protected virtual InputObjectGraphType ToInputObjectType(GraphQLInputObjectTypeDefinition inputDef)
        {
            var type = new InputObjectGraphType
            {
                Name = inputDef.Name.Value
            };

            ApplyDeprecatedDirective(inputDef.Directives, reason =>
            {
                type.DeprecationReason = reason;
            });

            var fields = inputDef.Fields.Select(x => ToFieldType(type.Name, x));
            fields.Apply(f => type.AddField(f));

            return type;
        }

        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var type = new EnumerationGraphType
            {
                Name = enumDef.Name.Value
            };

            ApplyDeprecatedDirective(enumDef.Directives, reason =>
            {
                type.DeprecationReason = reason;
            });

            var values = enumDef.Values.Select(ToEnumValue);
            values.Apply(type.AddValue);
            return type;
        }

        protected virtual DirectiveGraphType ToDirective(GraphQLDirectiveDefinition directiveDef)
        {
            var locations = directiveDef.Locations.Select(l => ToDirectiveLocation(l.Value));
            var directive = new DirectiveGraphType(directiveDef.Name.Value, locations);

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
                return (DirectiveLocation) result;
            }

            throw new ExecutionError($"{name} is an unknown directive location");
        }

        private EnumValueDefinition ToEnumValue(GraphQLEnumValueDefinition valDef)
        {
            var val = new EnumValueDefinition
            {
                Value = valDef.Name.Value,
                Name = valDef.Name.Value
            };

            ApplyDeprecatedDirective(valDef.Directives, reason =>
            {
                val.DeprecationReason = reason;
            });

            return val;
        }

        protected virtual QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            var arg = new QueryArgument(type)
            {
                Name = inputDef.Name.Value,
                DefaultValue = ToValue(inputDef.DefaultValue),
                ResolvedType = ToGraphType(inputDef.Type)
            };

            return arg;
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
            }

            throw new ArgumentOutOfRangeException($"Unknown GraphQL type {astType.Kind}");
        }

        private object ToValue(GraphQLValue source)
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
                    obj.Fields.Apply(f =>
                    {
                        values[f.Name.Value] = ToValue(f.Value);
                    });

                    return values;
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    Debug.Assert(list != null, nameof(list) + " != null");
                    var values = list.Values.Select(ToValue).ToArray();
                    return values;
                }
            }

            throw new ExecutionError($"Unsupported value type {source.Kind}");
        }

        protected virtual void CopyMetadata(IProvideMetadata target, IProvideMetadata source)
        {
            source.Metadata.Apply(kv =>
            {
                target.Metadata[kv.Key] = kv.Value;
            });
        }
    }

    internal static class SchemaExtensions
    {
        public static GraphQLDirective Directive(this IEnumerable<GraphQLDirective> directives, string name)
        {
            return directives?.FirstOrDefault(
                x => string.Equals(x.Name.Value, name, StringComparison.OrdinalIgnoreCase));
        }

        public static GraphQLArgument Argument(this IEnumerable<GraphQLArgument> arguments, string name)
        {
            return arguments?.FirstOrDefault(
                x => string.Equals(x.Name.Value, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
