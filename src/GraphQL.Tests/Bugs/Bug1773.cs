using System;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1773
    public class Bug1773 : QueryTestBase<Bug1773Schema>
    {
        private void AssertQueryWithError(string query, string result, string message, int line, int column, object[] path, Exception exception = null, string code = null, string inputs = null, string localizedMessage = null)
        {
            var error = exception == null ? new ExecutionError(message) : new ExecutionError(message, exception);
            if (line != 0)
                error.AddLocation(line, column);
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
            AssertQueryWithError("{testListNullInvalid}", "{\"testListNullInvalid\": null}", "Error trying to resolve field 'testListNullInvalid'.", 1, 2, new[] { "testListNullInvalid" },
                new InvalidOperationException("Cannot return a null member within a non-null list for list index 0."));
        }

        [Fact]
        public void list_throws_for_invalid_type()
        {
            // TODO: does not yet fully meet spec (does not return members of lists that are able to be serialized, with nulls and individual errors for unserializable values)
            AssertQueryWithError("{testListInvalidType}", "{\"testListInvalidType\": null}", "Error trying to resolve field 'testListInvalidType'.", 1, 2, new[] { "testListInvalidType" },
                new InvalidOperationException("Unable to serialize 'test' to 'Int' for list index 0."));
        }

        [Fact]
        public void list_throws_for_invalid_type_when_conversion_returns_null()
        {
            // TODO: does not yet fully meet spec (does not return members of lists that are able to be serialized, with nulls and individual errors for unserializable values)
            AssertQueryWithError("{testListInvalidType2}", "{\"testListInvalidType2\": null}", "Error trying to resolve field 'testListInvalidType2'.", 1, 2, new[] { "testListInvalidType2" },
                new InvalidOperationException("Unable to serialize 'test' to 'Bug1773Enum' for list index 0."));
        }

        [Fact]
        public void throws_for_invalid_type()
        {
            // in this case, the conversion threw a FormatException
            AssertQueryWithError("{testInvalidType}", "{\"testInvalidType\": null}", "Error trying to resolve field 'testInvalidType'.", 1, 2, new[] { "testInvalidType" },
                new InvalidOperationException("Unable to serialize 'test' to 'Int'."));
        }

        [Fact]
        public void throws_for_invalid_type_when_conversion_returns_null()
        {
            // in this case, the converstion returned null, and GraphQL threw an InvalidOperationException
            AssertQueryWithError("{testInvalidType2}", "{\"testInvalidType2\": null}", "Error trying to resolve field 'testInvalidType2'.", 1, 2, new[] { "testInvalidType2" },
                new InvalidOperationException("Unable to serialize 'test' to 'Bug1773Enum'."));
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
            Field<ListGraphType<IntGraphType>>("testListValid", resolve: context => new object[] { 123 });
            Field<ListGraphType<IntGraphType>>("testListInvalid", resolve: context => 123);
            Field<ListGraphType<IntGraphType>>("testListInvalidType", resolve: context => new object[] { "test" });
            Field<ListGraphType<EnumerationGraphType<Bug1773Enum>>>("testListInvalidType2", resolve: context => new object[] { "test" });
            Field<ListGraphType<NonNullGraphType<IntGraphType>>>("testListNullValid", resolve: context => new object[] { 123 });
            Field<ListGraphType<NonNullGraphType<IntGraphType>>>("testListNullInvalid", resolve: context => new object[] { null });
            Field<IntGraphType>("testNullValid", resolve: context => null);
            Field<NonNullGraphType<IntGraphType>>("testNullInvalid", resolve: context => null);
            Field<IntGraphType>("testInvalidType", resolve: context => "test");
            Field<EnumerationGraphType<Bug1773Enum>>("testInvalidType2", resolve: context => "test");
        }
    }

    public enum Bug1773Enum
    {
        Hello
    }
}
