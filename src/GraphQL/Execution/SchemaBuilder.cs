using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQLParser.AST.OperationType;

namespace GraphQL.Execution
{
    public class SchemaBuilder
    {
        private readonly IDictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();
        private readonly LightweightCache<string, IDictionary<string, IFieldResolver>> _resolvers;

        public SchemaBuilder()
        {
            _resolvers = new LightweightCache<string, IDictionary<string, IFieldResolver>>(type => new Dictionary<string, IFieldResolver>());
        }

        public void Resolver<TSourceType, TReturnType>(string type, string field, Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            Resolver(type, field, new FuncFieldResolver<TSourceType, TReturnType>(resolver));
        }

        public void Resolver<TReturnType>(string type, string field, Func<ResolveFieldContext, TReturnType> resolver)
        {
            Resolver(type, field, new FuncFieldResolver<TReturnType>(resolver));
        }

        public void Resolver(string type, string field, IFieldResolver resolver)
        {
            _resolvers[type][field] = resolver;
        }

        public ISchema Build(string typeDefs)
        {
            var document = Parse(typeDefs);
            return BuildSchemaFrom(document);
        }

        private static GraphQLDocument Parse(string document)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(document));
            return ast;
        }

        public ISchema BuildSchemaFrom(GraphQLDocument document)
        {
            var schema = new Schema();

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
                }
            }

            if (schemaDef != null)
            {
                foreach (var op in schemaDef.OperationTypes)
                {
                    var typeName = op.Type.Name.Value;
                    var type = _types[typeName] as IObjectGraphType;

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
                if (_types.ContainsKey("Query"))
                {
                    schema.Query = _types["Query"] as IObjectGraphType;
                }

                if (_types.ContainsKey("Mutation"))
                {
                    schema.Mutation = _types["Mutation"] as IObjectGraphType;
                }

                if (_types.ContainsKey("Subscription"))
                {
                    schema.Subscription = _types["Subscription"] as IObjectGraphType;
                }
            }

            var typeList = _types.Values.ToArray();
            typeList.Apply(schema.RegisterType);

            return schema;
        }

        public IObjectGraphType ToObjectGraphType(GraphQLObjectTypeDefinition astType)
        {
            var handlers = _resolvers[astType.Name.Value];

            var type = new ObjectGraphType();
            type.Name = astType.Name.Value;

            var fields = astType.Fields.Select(ToFieldType);
            fields.Apply(f =>
            {
                handlers.TryGetValue(f.Name, out IFieldResolver resolver);
                f.Resolver = resolver;
                type.AddField(f);
            });

            var interfaces = astType
                .Interfaces
                .Select(i => new GraphQLTypeReference(i.Name.Value))
                .ToList();
            interfaces.Apply(type.AddResolvedInterface);

            return type;
        }

        public FieldType ToFieldType(GraphQLFieldDefinition fieldDef)
        {
            var field = new FieldType();
            field.Name = fieldDef.Name.Value;
            field.ResolvedType = ToGraphType(fieldDef.Type);

            var args = fieldDef.Arguments.Select(ToArguments);
            field.Arguments = new QueryArguments(args);

            return field;
        }

        public InterfaceGraphType ToInterfaceType()
        {
            var type = new InterfaceGraphType();
            return type;
        }

        public QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            var arg = new QueryArgument(type);
            arg.Name = inputDef.Name.Value;
            arg.DefaultValue = ToValue(inputDef.DefaultValue);

            return arg;
        }

        public IGraphType ToGraphType(GraphQLType astType)
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
                    _types.TryGetValue(namedType.Name.Value, out IGraphType type);
                    return type ?? new GraphQLTypeReference(namedType.Name.Value);
                }
            }

            throw new ArgumentOutOfRangeException($"Unknown GraphQL type {astType.Kind}");
        }

        public object ToValue(GraphQLValue source)
        {
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
    }
}
