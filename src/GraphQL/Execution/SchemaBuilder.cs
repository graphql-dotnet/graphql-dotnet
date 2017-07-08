using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQLParser.AST.OperationType;

namespace GraphQL.Execution
{
    public class SchemaBuilder
    {
        private readonly IDictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();

        public ISchema Build(string typeDefs)
        {
            var document = Parse(typeDefs);
            return BuildAstSchema(document);
        }

        private static GraphQLDocument Parse(string document)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var ast = parser.Parse(new Source(document));
            return ast;
        }

        public ISchema BuildAstSchema(GraphQLDocument document)
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
            var type = new ObjectGraphType();
            type.Name = astType.Name.Value;

            var fields = astType.Fields.Select(ToFieldType);
            fields.Apply(f => type.AddField(f));
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

        public QueryArgument ToArguments(GraphQLInputValueDefinition inputDef)
        {
            var type = ToGraphType(inputDef.Type);

            var arg = new QueryArgument(type);
            arg.Name = inputDef.Name.Value;
            arg.DefaultValue = ToDefaultValue(inputDef.DefaultValue);

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

        public object ToDefaultValue(GraphQLValue astValue)
        {
            if (astValue.Kind == ASTNodeKind.IntValue)
            {
                return ((GraphQLScalarValue) astValue).Value;
            }

            return null;
        }
    }
}

