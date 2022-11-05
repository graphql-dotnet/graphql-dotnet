using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Tests.Errors;

public class ErrorExtensionsTests : QueryTestBase<ErrorExtensionsTests.TestSchema>
{
    [Fact]
    public void should_add_extension_object_when_exception_is_thrown_with_error_code()
    {
        string query = "{ firstSync }";
        string code = "FIRST";

        var errors = new ExecutionErrors();
        var error = new ExecutionError("Error trying to resolve field 'firstSync'.", new SystemException("Just inner exception 1", new DllNotFoundException("just inner exception 2")))
        {
            Code = code
        };
        error.AddLocation(new Location(1, 3));
        error.Path = new[] { "firstSync" };
        errors.Add(error);

        var expectedResult = @"{ ""firstSync"": null}";

        AssertQuery(query, CreateQueryResult(expectedResult, errors), null, null);
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "Query";

            Field<StringGraphType>("firstSync")
                .Resolve(_ => throw new FirstException("Exception from synchronous resolver", new SystemException("Just inner exception 1", new DllNotFoundException("just inner exception 2"))));

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

        public FirstException(string message, Exception innerException)
            : base(message, innerException)
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
