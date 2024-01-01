using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/pull/1772
// https://github.com/graphql-dotnet/graphql-dotnet/issues/3318 and https://github.com/graphql-dotnet/graphql-dotnet/pull/3870
public class Bug1772 : QueryTestBase<Bug1772Schema>
{
    [Theory]
    [InlineData("firstQuery")]
    [InlineData("secondQuery")]
    public async Task DocumentExecuter_works_for_valid_operation(string? operationName)
    {
        var de = new DocumentExecuter();
        var valid = await de.ExecuteAsync(new ExecutionOptions
        {
            Query = "query firstQuery {test} query secondQuery {test}",
            Schema = Schema,
            OperationName = operationName,
        });
        valid.ShouldNotBeNull();
        valid.Data.ShouldNotBeNull();
        valid.Errors.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
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
        });
        result.ShouldNotBeNull();
        result.Data.ShouldBeNull();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<InvalidOperationNameError>();
        result.Errors[0].Message.ShouldBe("Document does not contain an operation named '" + operationName + "'.");
        result.Errors[0].InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task DocumentExecuter_throws_for_multiple_operations()
    {
        var de = new DocumentExecuter();
        var result = await de.ExecuteAsync(new ExecutionOptions()
        {
            Query = "query firstQuery {test} query secondQuery {test}",
            Schema = Schema,
            OperationName = null,
        });
        result.ShouldNotBeNull();
        result.Data.ShouldBeNull();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NoOperationNameError>();
        result.Errors[0].Message.ShouldBe("Document contains more than one operation, but the operation name was not specified.");
        result.Errors[0].InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task DocumentExecuter_throws_for_no_operations()
    {
        var de = new DocumentExecuter();
        var result = await de.ExecuteAsync(new ExecutionOptions()
        {
            Query = "",
            Schema = Schema,
            OperationName = null,
        });
        result.ShouldNotBeNull();
        result.Data.ShouldBeNull();
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NoOperationError>();
        result.Errors[0].Message.ShouldBe("Document does not contain any operations.");
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
        Field<StringGraphType>("Test").Resolve(_ => "ok");
    }
}
