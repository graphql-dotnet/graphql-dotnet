using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue3370_LiteralObjectsWithNoEntriesDoNotValidate
{
    [Fact]
    public async Task ValidatesLiteralObjectsCorrectly()
    {
        var schema = new Schema { Query = new TestQuery() };

        var result1 = await schema.ExecuteAsync(options => options.Query = "{test(input: {}) { stringValue }}").ConfigureAwait(false);
        result1.ShouldBeCrossPlatJson("""{"errors":[{"message":"Argument \u0027input\u0027 has invalid value. Missing required field \u0027prop1\u0027 of type \u0027String\u0027.","locations":[{"line":1,"column":7}],"extensions":{"code":"ARGUMENTS_OF_CORRECT_TYPE","codes":["ARGUMENTS_OF_CORRECT_TYPE"],"number":"5.6.1"}}]}""");
    }

    [Fact]
    public async Task ValidatesVariableObjectsCorrectly()
    {
        var schema = new Schema { Query = new TestQuery() };

        var result2 = await schema.ExecuteAsync(options =>
        {
            options.Query = "query($input: TestInputType!){test(input: $input) { stringValue }}";
            options.Variables = @"{ ""input"": {}}".ToInputs();
        }).ConfigureAwait(false);
        result2.ShouldBeCrossPlatJson("""{"errors":[{"message":"Variable \u0027$input.prop1\u0027 is invalid. No value provided for a non-null variable.","locations":[{"line":1,"column":7}],"extensions":{"code":"INVALID_VALUE","codes":["INVALID_VALUE"],"number":"5.8"}}]}""");
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "testQuery";
            Description = "Test description";

            Field<TestResponseType>("test")
                .Arguments(new QueryArgument<NonNullGraphType<TestInputType>> { Name = "input" })
                .Resolve(_ => new TestResponse());
        }
    }

    public class TestResponseType : ObjectGraphType<TestResponse>
    {
        public TestResponseType()
        {
            Field<StringGraphType>("stringValue");
        }
    }

    public class TestResponse
    {
        public string StringValue { get; set; } = "Test";
    }

    public class TestInputType : InputObjectGraphType<TestInput>
    {
        public TestInputType()
        {
            Field<NonNullGraphType<StringGraphType>>("prop1");
        }
    }

    public class TestInput
    {
        public string Prop1 { get; set; }
    }
}
