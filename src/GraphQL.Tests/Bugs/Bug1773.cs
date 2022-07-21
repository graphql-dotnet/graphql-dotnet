using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/pulls/1773
public class Bug1773 : QueryTestBase<Bug1773Schema>
{
    private void AssertQueryWithError(string query, string result, string message, int line, int column, object[] path, Exception exception = null, string code = null, string inputs = null, string localizedMessage = null)
    {
        var error = exception == null ? new ExecutionError(message) : new ExecutionError(message, exception);
        if (line != 0)
            error.AddLocation(new Location(line, column));
        error.Path = path;
        if (code != null)
            error.Code = code;
        var expected = CreateQueryResult(result, new ExecutionErrors { error });
        var actualResult = AssertQueryIgnoreErrors(query, expected, inputs?.ToInputs(), renderErrors: true, expectedErrorCount: 1);
        if (exception != null)
        {
            Assert.Equal(exception.GetType(), actualResult.Errors[0].InnerException.GetType());

            if (localizedMessage != null && actualResult.Errors[0].InnerException.Message == localizedMessage)
                return;

            Assert.Equal(exception.Message, actualResult.Errors[0].InnerException.Message);
        }
    }

    [Fact]
    public void list_valid()
    {
        AssertQuerySuccess("{testListValid}", "{\"testListValid\": [123]}");
    }

    [Fact]
    public void list_throws_when_not_ienumerable()
    {
        AssertQueryWithError("{testListInvalid}", "{\"testListInvalid\": null}", "Error trying to resolve field 'testListInvalid'.", 1, 2, new[] { "testListInvalid" },
            new InvalidOperationException("Expected an IEnumerable list though did not find one. Found: Int32"));
    }

    [Fact]
    public void nonnull_list_valid()
    {
        AssertQuerySuccess("{testListNullValid}", "{\"testListNullValid\": [123]}");
    }

    [Fact]
    public void nonnull_list_throws_when_null()
    {
        AssertQueryWithError("{testListNullInvalid}", "{\"testListNullInvalid\": null}", "Error trying to resolve field 'testListNullInvalid'.", 1, 2, new object[] { "testListNullInvalid", 0 },
            new InvalidOperationException("Cannot return null for a non-null type. Field: testListNullInvalid, Type: Int!."));
    }

    [Fact]
    public void list_throws_for_invalid_type()
    {
        AssertQueryWithError("{testListInvalidType}", "{\"testListInvalidType\": [ null ]}", "Error trying to resolve field 'testListInvalidType'.", 1, 2, new object[] { "testListInvalidType", 0 },
            new InvalidOperationException("Unable to convert 'test' to the scalar type 'Int'"));
    }

    [Fact]
    public void list_throws_for_invalid_type_when_conversion_returns_null()
    {
        AssertQueryWithError("{testListInvalidType2}", "{\"testListInvalidType2\": [ null ]}", "Error trying to resolve field 'testListInvalidType2'.", 1, 2, new object[] { "testListInvalidType2", 0 },
            new InvalidOperationException("Unable to serialize 'test' to the scalar type 'Bug1773Enum'."));
    }

    [Fact]
    public void throws_for_invalid_type()
    {
        // in this case, the conversion threw a FormatException
        AssertQueryWithError("{testInvalidType}", "{\"testInvalidType\": null}", "Error trying to resolve field 'testInvalidType'.", 1, 2, new[] { "testInvalidType" },
            new InvalidOperationException("Unable to convert 'test' to the scalar type 'Int'"));
    }

    [Fact]
    public void throws_for_invalid_type_when_conversion_returns_null()
    {
        // in this case, the conversion returned null, and GraphQL threw an InvalidOperationException
        AssertQueryWithError("{testInvalidType2}", "{\"testInvalidType2\": null}", "Error trying to resolve field 'testInvalidType2'.", 1, 2, new[] { "testInvalidType2" },
            new InvalidOperationException("Unable to serialize 'test' to the scalar type 'Bug1773Enum'."));
    }

    [Fact]
    public void list_with_null_element_for_invalid_type_when_conversion_returns_null()
    {
        AssertQueryWithError("{testListInvalidType3}", "{\"testListInvalidType3\": [null, \"HELLO\"]}", "Error trying to resolve field 'testListInvalidType3'.", 1, 2, new object[] { "testListInvalidType3", 0 },
            new InvalidOperationException("Unable to serialize 'test' to the scalar type 'Bug1773Enum'."));
    }
}

public class Bug1773Schema : Schema
{
    public Bug1773Schema()
    {
        Query = new Bug1773Query();
    }
}

public class Bug1773Query : ObjectGraphType
{
    public Bug1773Query()
    {
        Field<ListGraphType<IntGraphType>>("testListValid").Resolve(_ => new object[] { 123 });
        Field<ListGraphType<IntGraphType>>("testListInvalid").Resolve(_ => 123);
        Field<ListGraphType<IntGraphType>>("testListInvalidType").Resolve(_ => new object[] { "test" });
        Field<ListGraphType<EnumerationGraphType<Bug1773Enum>>>("testListInvalidType2").Resolve(_ => new object[] { "test" });
        Field<ListGraphType<EnumerationGraphType<Bug1773Enum>>>("testListInvalidType3").Resolve(_ => new object[] { "test", Bug1773Enum.Hello });
        Field<ListGraphType<NonNullGraphType<IntGraphType>>>("testListNullValid").Resolve(_ => new object[] { 123 });
        Field<ListGraphType<NonNullGraphType<IntGraphType>>>("testListNullInvalid").Resolve(_ => new object[] { null });
        Field<IntGraphType>("testNullValid").Resolve(_ => null);
        Field<NonNullGraphType<IntGraphType>>("testNullInvalid").Resolve(_ => null);
        Field<IntGraphType>("testInvalidType").Resolve(_ => "test");
        Field<EnumerationGraphType<Bug1773Enum>>("testInvalidType2").Resolve(_ => "test");
    }
}

public enum Bug1773Enum
{
    Hello
}
