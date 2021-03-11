using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2387
    public class Issue2387_OverrideBuiltInScalars : QueryTestBase<Issue2387_OverrideBuiltInScalars.MySchema>
    {
        [Fact]
        public void codefirst_output()
        {
            AssertQuerySuccess("{ testOutput }", "{\"testOutput\": 124}");
        }

        [Fact]
        public void codefirst_parseliteral()
        {
            AssertQuerySuccess("{ testInput(arg:123) }", "{\"testInput\": \"122\"}");
        }

        [Fact]
        public void codefirst_parsevalue()
        {
            AssertQuerySuccess("query ($arg: Int) { testInput(arg:$arg) }", "{\"testInput\": \"122\"}", "{\"arg\":123}".ToInputs());
        }

        [Fact]
        public void codefirst_nonnull_output()
        {
            AssertQuerySuccess("{ testNonNullOutput }", "{\"testNonNullOutput\": 124}");
        }

        [Fact]
        public void codefirst_nonnull_parseliteral()
        {
            AssertQuerySuccess("{ testNonNullInput(arg:123) }", "{\"testNonNullInput\": \"122\"}");
        }

        [Fact]
        public void codefirst_nonnull_parsevalue()
        {
            AssertQuerySuccess("query ($arg: Int!) { testNonNullInput(arg:$arg) }", "{\"testNonNullInput\": \"122\"}", "{\"arg\":123}".ToInputs());
        }

        [Fact]
        public async Task schemafirst_output()
        {
            var schema = BuildSchemaFirst();
            var json = await schema.ExecuteAsync(_ =>
            {
                _.Query = "{ testOutput }";
                _.Root = new SchemaFirstRoot();
            });
            json.ShouldBeCrossPlatJson("{\"data\":{\"testOutput\": 124}}");
        }

        [Fact]
        public async Task schemafirst_parseliteral()
        {
            var schema = BuildSchemaFirst();
            var json = await schema.ExecuteAsync(_ =>
            {
                _.Query = "{ testInput(arg:123) }";
                _.Root = new SchemaFirstRoot();
            });
            json.ShouldBeCrossPlatJson("{\"data\":{\"testInput\": \"122\"}}");
        }

        [Fact]
        public async Task schemafirst_parsevalue()
        {
            var schema = BuildSchemaFirst();
            var json = await schema.ExecuteAsync(_ =>
            {
                _.Query = "query ($arg: Int!) { testInput(arg:$arg) }";
                _.Inputs = "{\"arg\":123}".ToInputs();
            });
            json.ShouldBeCrossPlatJson("{\"data\":{\"testInput\": \"122\"}}");
        }

        private Schema BuildSchemaFirst()
        {
            var typeDefs = @"
type Query {
  testOutput: Int!
  testInput(arg: Int!): String!
}";
            var schema = GraphQL.Types.Schema.For(typeDefs, config => config.Types.Include<SchemaFirstRoot>());
            schema.RegisterType(new MyIntGraphType());
            return schema;
        }

        [GraphQLMetadata("Query")]
        public class SchemaFirstRoot
        {
            public int TestOutput()
            {
                return 123;
            }
            public string TestInput(IResolveFieldContext context)
            {
                return context.GetArgument<int>("arg").ToString();
            }
        }

        public class MySchema : Schema
        {
            public MySchema()
            {
                Query = new MyQuery();
                RegisterType(new MyIntGraphType());
            }
        }

        public class MyQuery : ObjectGraphType
        {
            public MyQuery()
            {
                Field<IntGraphType>("testOutput", resolve: context => 123);
                Field<StringGraphType>("testInput",
                    arguments: new QueryArguments {
                        new QueryArgument<IntGraphType> { Name = "arg" }
                    },
                    resolve: context => context.GetArgument<int>("arg").ToString());
                Field<NonNullGraphType<IntGraphType>>("testNonNullOutput", resolve: context => 123);
                Field<StringGraphType>("testNonNullInput",
                    arguments: new QueryArguments {
                        new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "arg" }
                    },
                    resolve: context => context.GetArgument<int>("arg").ToString());
            }
        }

        public class MyIntGraphType : IntGraphType
        {
            public MyIntGraphType()
            {
                Name = "Int";
            }

            public override object ParseLiteral(IValue value) {
                var ret = base.ParseLiteral(value);
                return ret is int i ? i - 1 : ret;
            }

            public override object ParseValue(object value)
                => value is int i ? i - 1 : value;

            public override object Serialize(object value)
                => value is int i ? (object)(i + 1) : null;
        }
    }
}
