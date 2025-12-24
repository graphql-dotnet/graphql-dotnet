using GraphQL.Types;

namespace GraphQL.Tests.Execution;

[Collection("StaticTests")]
public class ExecutionStrategyTests
{
    [Theory]
    [InlineData("query { test }", "Schema is not configured for queries", true)]
    [InlineData("mutation { test }", "Schema is not configured for mutations", true)]
    [InlineData("subscription { test }", "Schema is not configured for subscriptions", true)]
    [InlineData("mutation { test }", "Schema is not configured for mutations", false)]
    [InlineData("subscription { test }", "Schema is not configured for subscriptions", false)]
    public async Task Refuses_Null_Root_Type(string query, string expectedErrorMessage, bool noQuery)
    {
        try
        {
            var schema = new Schema();
            if (noQuery)
            {
                GlobalSwitches.RequireRootQueryType = false;
            }
            else
            {
                schema.Query = new DummyType();
            }

            schema.Initialize();
            var executer = new DocumentExecuter();
            var result = await executer.ExecuteAsync(new ExecutionOptions
            {
                Query = query,
                Schema = schema,
            });
            result.Data.ShouldBeNull();
            //TODO: the document should fail validation and Executed should return false
            //see: https://github.com/graphql/graphql-spec/pull/955
            result.Executed.ShouldBeTrue();
            result.Errors.ShouldHaveSingleItem().Message.ShouldBe(expectedErrorMessage);
        }
        finally
        {
            GlobalSwitches.RequireRootQueryType = true;
        }
    }
}
