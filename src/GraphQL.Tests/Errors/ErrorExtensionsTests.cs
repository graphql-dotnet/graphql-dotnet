using GraphQL.Types;
using GraphQL.Validation;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class ErrorExtensionsTests : QueryTestBase<ErrorExtensionsTests.TestSchema>
    {
        [Fact]
        public async Task should_add_extension_object_when_exception_is_thrown_with_error_code()
        {
            string query = "{ firstSync }";
            string code = "FIRST";

            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
            });

            var errors = new ExecutionErrors();
            var error = new ValidationError(query, code, "Error trying to resolve firstSync.");
            error.AddLocation(1, 3);
            error.Path = new[] { "firstSync" };
            errors.Add(error);

            var expectedResult = "{firstSync: null}";

            AssertQuery(query, CreateQueryResult(expectedResult, errors), null, null);
        }

        [Fact]
        public async Task should_not_add_extension_object_when_exception_is_thrown_without_error_code()
        {
            string query = "{ uncodedSync }";

            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
            });

            var errors = new ExecutionErrors();
            var error = new ExecutionError("Error trying to resolve uncodedSync.");
            error.AddLocation(1, 3);
            error.Path = new[] { "uncodedSync" };
            errors.Add(error);

            var expectedResult = "{uncodedSync: null}";

            AssertQuery(query, CreateQueryResult(expectedResult, errors), null, null);
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";
                Field<StringGraphType>(
                    "firstSync",
                    resolve: _ => throw new FirstException("Exception from synchronous resolver")
                );
                FieldAsync<StringGraphType>(
                    "firstAsync",
                    resolve: async _ => throw new FirstException("Exception from asynchronous resolver")
                );
                Field<StringGraphType>(
                    "secondSync",
                    resolve: _ => throw new SecondTestException("Exception from synchronous resolver")
                );
                FieldAsync<StringGraphType>(
                    "secondAsync",
                    resolve: async _ => throw new SecondTestException("Exception from asynchronous resolver")
                );
                Field<StringGraphType>(
                    "uncodedSync",
                    resolve: _ => throw new Exception("Exception from synchronous resolver")
                );
                FieldAsync<StringGraphType>(
                    "uncodedAsync",
                    resolve: async _ => throw new Exception("Exception from asynchronous resolver")
                );
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
}
