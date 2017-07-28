using System;
using System.Collections.Generic;
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

        public TypeSettings Types { get; } = new TypeSettings();

        public void RegisterType(IGraphType type)
        {
            _types[type.Name] = type;
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
            var schema = new Schema();
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

        protected virtual IObjectGraphType ToObjectGraphType(GraphQLObjectTypeDefinition astType)
        {
            var typeConfig = Types.ConfigFor(astType.Name.Value);

            var type = new ObjectGraphType();
            type.Name = astType.Name.Value;
            type.Description = typeConfig.Description;
            type.IsTypeOf = typeConfig.IsTypeOf;

            CopyMetadata(type, typeConfig);

            var fields = astType.Fields.Select(ToFieldType);
            fields.Apply(f =>
            {
                f.Resolver = typeConfig.ResolverFor(f.Name);
                type.AddField(f);
            });

            var interfaces = astType
                .Interfaces
                .Select(i => new GraphQLTypeReference(i.Name.Value))
                .ToList();
            interfaces.Apply(type.AddResolvedInterface);

            return type;
        }

        protected virtual FieldType ToFieldType(GraphQLFieldDefinition fieldDef)
        {
            var field = new FieldType();
            field.Name = fieldDef.Name.Value;
            field.ResolvedType = ToGraphType(fieldDef.Type);

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);

            return field;
        }

        protected virtual FieldType ToFieldType(GraphQLInputValueDefinition inputDef)
        {
            var field = new FieldType();
            field.Name = inputDef.Name.Value;
            field.ResolvedType = ToGraphType(inputDef.Type);
            field.DefaultValue = ToValue(inputDef.DefaultValue);

            return field;
        }

        protected virtual InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var typeConfig = Types.ConfigFor(interfaceDef.Name.Value);

            var type = new InterfaceGraphType();
            type.Name = interfaceDef.Name.Value;
            type.Description = typeConfig.Description;
            type.ResolveType = typeConfig.ResolveType;

            CopyMetadata(type, typeConfig);

            var fields = interfaceDef.Fields.Select(ToFieldType);
            fields.Apply(f => type.AddField(f));

            return type;
        }

        protected virtual UnionGraphType ToUnionType(GraphQLUnionTypeDefinition unionDef)
        {
            var typeConfig = Types.ConfigFor(unionDef.Name.Value);

            var type = new UnionGraphType();
            type.Name = unionDef.Name.Value;
            type.Description = typeConfig.Description;
            type.ResolveType = typeConfig.ResolveType;

            CopyMetadata(type, typeConfig);

            var possibleTypes = unionDef.Types.Select(x => GetType(x.Name.Value));
            possibleTypes.Apply(x => type.AddPossibleType(x as IObjectGraphType));
            return type;
        }

        protected virtual InputObjectGraphType ToInputObjectType(GraphQLInputObjectTypeDefinition inputDef)
        {
            var type = new InputObjectGraphType();
            type.Name = inputDef.Name.Value;
            var fields = inputDef.Fields.Select(ToFieldType);

            fields.Apply(f => type.AddField(f));

            return type;
        }

        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var type = new EnumerationGraphType();
            type.Name = enumDef.Name.Value;
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
            var val = new EnumValueDefinition();
            val.Value = valDef.Name.Value;
            val.Name = valDef.Name.Value;
            return val;
        }

        protected virtual QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            var arg = new QueryArgument(type);
            arg.Name = inputDef.Name.Value;
            arg.DefaultValue = ToValue(inputDef.DefaultValue);
            arg.ResolvedType = ToGraphType(inputDef.Type);

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
                case ASTNodeKind.StringValue:
                {
                    var str = source as GraphQLScalarValue;
                    return str.Value;
                }
                case ASTNodeKind.IntValue:
                {
                    var str = source as GraphQLScalarValue;

                    int intResult;
                    if (int.TryParse(str.Value, out intResult))
                    {
                        return intResult;
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    long longResult;
                    if (long.TryParse(str.Value, out longResult))
                    {
                        return longResult;
                    }

                    throw new ExecutionError($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = source as GraphQLScalarValue;
                    return double.Parse(str.Value);
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = source as GraphQLScalarValue;
                    return bool.Parse(str.Value);
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = source as GraphQLScalarValue;
                    return str.Value;
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = source as GraphQLObjectValue;
                    var values = new Dictionary<string, object>();

                    obj.Fields.Apply(f =>
                    {
                        values[f.Name.Value] = ToValue(f.Value);
                    });

                    return values;
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    var values = list.Values.Select(ToValue).ToArray();
                    return values;
                }
            }

            throw new ExecutionError($"Unsupported value type {source.Kind}");
        }

        protected virtual void CopyMetadata(IGraphType type, TypeConfig config)
        {
            config.Metadata.Apply(kv =>
            {
                type.Metadata[kv.Key] = kv.Value;
            });
        }
    }
}
