using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2392
public class Issue2392_OverrideBuiltInScalars_Alt : QueryTestBase<Issue2392_OverrideBuiltInScalars_Alt.MySchema>
{
    [Fact]
    public void replace_not_scalar_should_throw()
    {
        Schema.AllTypes["MyQuery"].ShouldBeOfType<MyQuery>();

        Should.Throw<InvalidOperationException>(() => Schema.ReplaceScalar(new BadScalar()));

        Schema.AllTypes["MyQuery"].ShouldBeOfType<MyQuery>();
    }

    [Fact]
    public void codefirst_output()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testOutput }", "{\"testOutput\": 124}");
    }

    [Fact]
    public void codefirst_parseliteral()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testInput(arg:123) }", "{\"testInput\": \"122\"}");
    }

    [Fact]
    public void codefirst_parsevalue()
    {
        Replace(Schema);
        AssertQuerySuccess("query ($arg: Int) { testInput(arg:$arg) }", "{\"testInput\": \"122\"}", "{\"arg\":123}".ToInputs());
    }

    [Fact]
    public void codefirst_output_string()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testOutputString }", "{\"testOutputString\": \"output-hello\"}");
    }

    [Fact]
    public void codefirst_parseliteral_string()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testInputString(arg:\"hello\") }", "{\"testInputString\": \"input-hello\"}");
    }

    [Fact]
    public void codefirst_parsevalue_string()
    {
        Replace(Schema);
        AssertQuerySuccess("query ($arg: String) { testInputString(arg:$arg) }", "{\"testInputString\": \"input-hello\"}", "{\"arg\":\"hello\"}".ToInputs());
    }

    [Fact]
    public void codefirst_nonnull_output()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testNonNullOutput }", "{\"testNonNullOutput\": 124}");
    }

    [Fact]
    public void codefirst_nonnull_parseliteral()
    {
        Replace(Schema);
        AssertQuerySuccess("{ testNonNullInput(arg:123) }", "{\"testNonNullInput\": \"122\"}");
    }

    [Fact]
    public void codefirst_nonnull_parsevalue()
    {
        Replace(Schema);
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

        Replace(schema);

        return schema;
    }

    private void Replace(Schema schema)
    {
        schema.AllTypes["Int"].ShouldBeOfType<IntGraphType>();
        schema.AllTypes["String"].ShouldBeOfType<StringGraphType>();

        schema.ReplaceScalar(new MyIntGraphType());
        schema.ReplaceScalar(new MyStringGraphType());

        schema.AllTypes["Int"].ShouldBeOfType<MyIntGraphType>();
        schema.AllTypes["String"].ShouldBeOfType<MyStringGraphType>();
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
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<IntGraphType>("testOutput").Resolve(_ => 123);
            Field<IdGraphType>("testInput")
                .Argument<IntGraphType>("arg")
                .Resolve(context => context.GetArgument<int>("arg").ToString());
            Field<NonNullGraphType<IntGraphType>>("testNonNullOutput").Resolve(_ => 123);
            Field<IdGraphType>("testNonNullInput")
                .Argument<NonNullGraphType<IntGraphType>>("arg")
                .Resolve(context => context.GetArgument<int>("arg").ToString());
            Field<StringGraphType>("testOutputString").Resolve(_ => "hello");
            Field<IdGraphType>("testInputString")
                .Argument<StringGraphType>("arg")
                .Resolve(context => context.GetArgument<string>("arg"));
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

    public class BadScalar : ScalarGraphType
    {
        public BadScalar()
        {
            Name = "MyQuery";
        }

        public override object ParseLiteral(GraphQLValue value) => throw new System.NotImplementedException();

        public override object ParseValue(object value) => throw new System.NotImplementedException();
    }
}
