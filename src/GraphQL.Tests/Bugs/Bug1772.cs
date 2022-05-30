using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/pulls/1772
public class Bug1772 : QueryTestBase<Bug1772Schema>
{
    [Theory]
    [InlineData("")]
    [InlineData((string)null)]
    [InlineData("firstQuery")]
    [InlineData("secondQuery")]
    public async Task DocumentExecuter_works_for_valid_operation(string operationName)
    {
        var de = new DocumentExecuter();
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            Query = "query firstQuery {test} query secondQuery {test}",
            Schema = Schema,
            OperationName = operationName,
        }).ConfigureAwait(false);
        valid.ShouldNotBeNull();
        valid.Data.ShouldNotBeNull();
        valid.Errors.ShouldBeNull();
    }

    [Theory]
    [InlineData("thirdQuery")]
    [InlineData("query")]
    [InlineData("test")]
    public async Task DocumentExecuter_throws_for_invalid_operation(string operationName)
    {
        var de = new DocumentExecuter();
        var result = await de.ExecuteAsync(new ExecutionOptions()
        {
            Query = "query firstQuery {test} query secondQuery {test}",
            Schema = Schema,
            OperationName = operationName
        }).ConfigureAwait(false);
        result.ShouldNotBeNull();
        result.Data.ShouldBeNull();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<InvalidOperationError>();
        result.Errors[0].Message.ShouldBe("Query does not contain operation '" + operationName + "'.");
        result.Errors[0].InnerException.ShouldBeNull();
    }
}

public class Bug1772Schema : Schema
{
    public Bug1772Schema()
    {
        Query = new Bug1772Query();
    }
}

public class Bug1772Query : ObjectGraphType
{
    public Bug1772Query()
    {
        Field<StringGraphType>("Test", resolve: context => "ok");
    }
}
