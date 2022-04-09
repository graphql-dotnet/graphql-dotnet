using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

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
    public void codefirst_output_string()
    {
        AssertQuerySuccess("{ testOutputString }", "{\"testOutputString\": \"output-hello\"}");
    }

    [Fact]
    public void codefirst_parseliteral_string()
    {
        AssertQuerySuccess("{ testInputString(arg:\"hello\") }", "{\"testInputString\": \"input-hello\"}");
    }

    [Fact]
    public void codefirst_parsevalue_string()
    {
        AssertQuerySuccess("query ($arg: String) { testInputString(arg:$arg) }", "{\"testInputString\": \"input-hello\"}", "{\"arg\":\"hello\"}".ToInputs());
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
        var json = await schema.ExecuteAsync(_ => _.Query = "{ testOutput }").ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testOutput\": 124}}");
    }

    [Fact]
    public async Task schemafirst_parseliteral()
    {
        var schema = BuildSchemaFirst();
        var json = await schema.ExecuteAsync(_ => _.Query = "{ testInput(arg:123) }").ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testInput\": \"122\"}}");
    }

    [Fact]
    public async Task schemafirst_parsevalue()
    {
        var schema = BuildSchemaFirst();
        var json = await schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($arg: Int!) { testInput(arg:$arg) }";
            _.Variables = "{\"arg\":123}".ToInputs();
        }).ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testInput\": \"122\"}}");
    }

    [Fact]
    public async Task schemafirst_output_string()
    {
        var schema = BuildSchemaFirst();
        var json = await schema.ExecuteAsync(_ => _.Query = "{ testOutputString }").ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testOutputString\": \"output-hello\"}}");
    }

    [Fact]
    public async Task schemafirst_parseliteral_string()
    {
        var schema = BuildSchemaFirst();
        var json = await schema.ExecuteAsync(_ => _.Query = "{ testInputString(arg:\"hello\") }").ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testInputString\": \"input-hello\"}}");
    }

    [Fact]
    public async Task schemafirst_parsevalue_string()
    {
        var schema = BuildSchemaFirst();
        var json = await schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($arg: String!) { testInputString(arg:$arg) }";
            _.Variables = "{\"arg\":\"hello\"}".ToInputs();
        }).ConfigureAwait(false);
        json.ShouldBeCrossPlatJson("{\"data\":{\"testInputString\": \"input-hello\"}}");
    }

    private Schema BuildSchemaFirst()
    {
        var typeDefs = @"
type Query {
  testOutput: Int!
  testInput(arg: Int!): ID!
  testOutputString: String!
  testInputString(arg: String!): ID!
}";
        var schema = GraphQL.Types.Schema.For(typeDefs, config => config.Types.Include<SchemaFirstRoot>());
        schema.RegisterType(new MyIntGraphType());
        schema.RegisterType<MyStringGraphType>();
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

        public string TestOutputString()
        {
            return "hello";
        }

        public string TestInputString(IResolveFieldContext context)
        {
            return context.GetArgument<string>("arg");
        }
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
            RegisterType(new MyIntGraphType());
            RegisterType(typeof(MyStringGraphType));
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<IntGraphType>("testOutput", resolve: context => 123);
            Field<IdGraphType>("testInput",
                arguments: new QueryArguments {
                    new QueryArgument<IntGraphType> { Name = "arg" }
                },
                resolve: context => context.GetArgument<int>("arg").ToString());
            Field<NonNullGraphType<IntGraphType>>("testNonNullOutput", resolve: context => 123);
            Field<IdGraphType>("testNonNullInput",
                arguments: new QueryArguments {
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "arg" }
                },
                resolve: context => context.GetArgument<int>("arg").ToString());
            Field<StringGraphType>("testOutputString", resolve: context => "hello");
            Field<IdGraphType>("testInputString",
                arguments: new QueryArguments
                {
                    new QueryArgument<StringGraphType> { Name = "arg" }
                },
                resolve: context => context.GetArgument<string>("arg"));
        }
    }

    public class MyIntGraphType : IntGraphType
    {
        public MyIntGraphType()
        {
            Name = "Int";
        }

        public override object ParseLiteral(GraphQLValue value)
        {
            var ret = base.ParseLiteral(value);
            return ret is int i ? i - 1 : ret;
        }

        public override object ParseValue(object value)
            => value is int i ? i - 1 : base.ParseValue(value);

        public override object Serialize(object value)
            => value is int i ? i + 1 : base.Serialize(value);
    }

    public class MyStringGraphType : StringGraphType
    {
        public MyStringGraphType()
        {
            Name = "String";
        }

        public override object ParseLiteral(GraphQLValue value)
            => value is GraphQLStringValue s ? "input-" + s.Value : base.ParseLiteral(value);

        public override object ParseValue(object value)
            => value is string s ? "input-" + s : base.ParseValue(value);

        public override object Serialize(object value)
            => value is string s ? "output-" + s : base.Serialize(value);
    }
}
