using GraphQL.Types;

namespace GraphQL.Tests.Errors;

public class ErrorCodeTests : QueryTestBase<ErrorCodeTests.TestSchema>
{
    [Theory]
    [InlineData("{ firstSync }", "FIRST")]
    [InlineData("{ secondSync }", "SECOND_TEST")]
    [InlineData("{ uncodedSync }", "")]
    public async Task should_show_code_when_exception_is_thrown_with_sync_field(string query, string code)
    {
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = query;
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Code.ShouldBe(code);
    }

    [Theory]
    [InlineData("{ firstAsync }", "FIRST")]
    [InlineData("{ secondAsync }", "SECOND_TEST")]
    [InlineData("{ uncodedAsync }", "")]
    public async Task should_show_code_when_exception_thrown_with_async_field(string query, string code)
    {
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = query;
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Code.ShouldBe(code);
    }

    [Fact]
    public async Task should_propagate_exception_data_when_exception_is_thrown_in_field_resolver()
    {
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = @"{ secondSync }";
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Data["Foo"].ShouldBe("Bar");
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "Query";

            Field<StringGraphType>("firstSync")
                .Resolve(_ => throw new FirstException("Exception from synchronous resolver"));

            Field<StringGraphType>("firstAsync")
                .ResolveAsync(_ => throw new FirstException("Exception from asynchronous resolver"));

            Field<StringGraphType>("secondSync")
                .Resolve(_ => throw new SecondTestException("Exception from synchronous resolver"));

            Field<StringGraphType>("secondAsync")
                .ResolveAsync(_ => throw new SecondTestException("Exception from asynchronous resolver"));

            Field<StringGraphType>("uncodedSync")
                .Resolve(_ => throw new Exception("Exception from synchronous resolver"));

            Field<StringGraphType>("uncodedAsync")
                .ResolveAsync(_ => throw new Exception("Exception from asynchronous resolver"));
        }
    }

    public class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new TestQuery();
        }
    }

    public class FirstException : Exception
    {
        public FirstException(string message)
            : base(message)
        {
        }
    }

    public class SecondTestException : Exception
    {
        public SecondTestException(string message)
            : base(message)
        {
            Data["Foo"] = "Bar";
        }
    }
}
