using GraphQL.Types;

namespace GraphQL.Tests.Execution;

// https://github.com/graphql-dotnet/graphql-dotnet/pulls/1735
public class DocumentExecuterExceptions : QueryTestBase<PR1735Schema>
{
    [Theory]
    [InlineData("")]                   // NO_OPERATION
    [InlineData("{ unknown }")]        // 5.2.1 (validation error)
    [InlineData("{ abcd")]             // SYNTAX_ERROR
    [InlineData("{ test(arg: 500) }")] // 5.3.3.1 (invalid type)
    public async Task DocumentExecuter_does_not_throw_for_invalid_queries(string query)
    {
        var de = new DocumentExecuter();
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = Schema,
            ThrowOnUnhandledException = true,
        }).ConfigureAwait(false);
        valid.ShouldNotBeNull();
        valid.Data.ShouldBeNull();
        valid.Errors.ShouldNotBeNull();
        valid.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DocumentExecuter_does_not_throw_for_invalid_variables()
    {
        var de = new DocumentExecuter();
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            Query = "query($arg: Byte!) { test(arg: $arg) }",
            Schema = Schema,
            Variables = "{\"arg\":500}".ToInputs(),
            ThrowOnUnhandledException = true,
        }).ConfigureAwait(false);
        valid.ShouldNotBeNull();
        valid.Data.ShouldBeNull();
        valid.Errors.ShouldNotBeNull();
        valid.Errors.Count.ShouldBe(1);
    }
}

public class PR1735Schema : Schema
{
    public PR1735Schema()
    {
        Query = new PR1735Query();
    }
}

public class PR1735Query : ObjectGraphType
{
    public PR1735Query()
    {
        Field<StringGraphType>("Test",
            resolve: context => "ok",
            arguments: new QueryArguments(new QueryArgument(typeof(ByteGraphType)) { Name = "arg" }));
    }
}
