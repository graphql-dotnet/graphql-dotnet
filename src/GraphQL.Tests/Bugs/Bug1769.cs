using System;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1769
    public class Bug1769 : QueryTestBase<Bug1769Schema>
    {
        private void AssertQueryWithError(string query, string result, string message, int line, int column, object[] path, Exception exception = null, string code = null, string inputs = null)
        {
            var error = exception == null ? new ExecutionError(message) : new ExecutionError(message, exception);
            if (line != 0)
                error.AddLocation(line, column);
            error.Path = path;
            if (code != null)
                error.Code = code;
            var expected = CreateQueryResult(result, new ExecutionErrors { error });
            AssertQueryIgnoreErrors(query, expected, inputs?.ToInputs(), renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public async Task DocumentExecuter_has_valid_options()
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = "{test}",
                Schema = Schema,
            });
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await de.ExecuteAsync(new ExecutionOptions()
                {
                    Query = null,
                    Schema = Schema,
                });
            });
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await de.ExecuteAsync(new ExecutionOptions()
                {
                    Query = "{test}",
                    Schema = null,
                });
            });
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await de.ExecuteAsync(new ExecutionOptions()
                {
                    Query = "{test}",
                    Schema = Schema,
                    FieldMiddleware = null,
                });
            });
        }

        [Fact]
        public void query_is_empty1() => AssertQueryWithError("", null, "Document does not contain any operations.", 0, 0, (object[])null, code: "NO_OPERATION");

        [Fact]
        public void query_is_whitespace2() => AssertQueryWithError("\t \t \r\n", null, "Document does not contain any operations.", 0, 0, (object[])null, code: "NO_OPERATION");

        [Fact]
        public void DocumentExecuter_cannot_have_null_constructor_parameters()
        {
            var valid1 = new DocumentExecuter();
            var valid2 = new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer());
            Assert.Throws<ArgumentNullException>(() => new DocumentExecuter(null, new DocumentValidator(), new ComplexityAnalyzer()));
            Assert.Throws<ArgumentNullException>(() => new DocumentExecuter(new GraphQLDocumentBuilder(), null, new ComplexityAnalyzer()));
            Assert.Throws<ArgumentNullException>(() => new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), null));
        }
    }

    public class Bug1769Schema : Schema
    {
        public Bug1769Schema()
        {
            Query = new Bug1769Query();
        }
    }

    public class Bug1769Query : ObjectGraphType
    {
        public Bug1769Query()
        {
            Field<StringGraphType>("Test", resolve: context => "ok");
        }
    }
}
