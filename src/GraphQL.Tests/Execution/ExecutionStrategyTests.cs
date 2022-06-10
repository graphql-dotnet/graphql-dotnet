using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class ExecutionStrategyTests
{
    [Theory]
    [InlineData("query { test }", "Schema is not configured for queries")]
    [InlineData("mutation { test }", "Schema is not configured for mutations")]
    [InlineData("subscription { test }", "Schema is not configured for subscriptions")]
    public async Task Refuses_Null_Root_Type(string query, string expectedErrorMessage)
    {
        var schema = new Schema();
        schema.Initialize();
        var executer = new DocumentExecuter();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = schema,
        }).ConfigureAwait(false);
        result.Data.ShouldBeNull();
        //TODO: the document should fail validation and Executed should return false
        //see: https://github.com/graphql/graphql-spec/pull/955
        result.Executed.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe(expectedErrorMessage);
    }
}
