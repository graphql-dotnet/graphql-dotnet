using GraphQL.Execution;
using GraphQL.Tests.Utilities;
using GraphQLParser;

namespace GraphQL.Tests.Errors;

public class UnhandledExceptionTests : SchemaBuilderTestBase
{
    [Fact]
    public void rethrows_unhandled_exception()
    {
        var def = @"
                type Query {
                  hello: Int
                }
            ";

        Builder.Types.Include<Query>();

        Should.Throw<AggregateException>(() =>
        {
            AssertQuery(_ =>
            {
                _.Query = "{ hello }";
                _.Definitions = def;
                _.ThrowOnUnhandledException = true;
            });
        });
    }

    [Fact]
    public void unhandled_exception_delegate_can_rethrow_custom_exception()
    {
        var def = @"
                type Query {
                  hello2: Int
                }
            ";

        Builder.Types.Include<Query>();

        var expectedError = new ExecutionError("Test error message");
        expectedError.AddLocation(new Location(1, 3));
        expectedError.Path = new[] { "hello2" };

        AssertQuery(options =>
        {
            options.Schema = Builder.Build(def);
            options.Query = "{ hello2 }";
            options.UnhandledExceptionDelegate = ctx =>
            {
                if (ctx.Exception is ApplicationException)
                {
                    ctx.Exception = new ExecutionError("Test error message");
                }
                return Task.CompletedTask;
            };
        }, new ExecutionResult { Errors = new ExecutionErrors { expectedError }, Data = new { hello2 = (object)null }, Executed = true });

    }

    [Fact]
    public void unhandled_exception_delegate_can_rethrow_custom_message_from_field_resolver()
    {
        var def = @"
                type Query {
                  hello2: Int
                }
            ";

        Builder.Types.Include<Query>();

        var expectedError = new ExecutionError("Test error message", new ApplicationException());
        expectedError.AddLocation(new Location(1, 3));
        expectedError.Path = new[] { "hello2" };

        AssertQuery(options =>
        {
            options.Schema = Builder.Build(def);
            options.Query = "{ hello2 }";
            options.UnhandledExceptionDelegate = ctx =>
            {
                if (ctx.Exception is ApplicationException)
                {
                    ctx.ErrorMessage = "Test error message";
                }
                return Task.CompletedTask;
            };
        }, new ExecutionResult { Errors = new ExecutionErrors { expectedError }, Data = new { hello2 = (object)null }, Executed = true });

    }

    [Fact]
    public void unhandled_exception_delegate_can_rethrow_custom_message_from_document_listener()
    {
        var def = @"
                type Query {
                  hello2: Int
                }
            ";

        Builder.Types.Include<Query>();

        var expectedError = new ExecutionError("Test error message", new ApplicationException2());

        AssertQuery(options =>
        {
            options.Schema = Builder.Build(def);
            options.Query = "{ hello2 }";
            options.Listeners.Add(new DocListener());
            options.UnhandledExceptionDelegate = ctx =>
            {
                if (ctx.Exception is ApplicationException2)
                {
                    ctx.ErrorMessage = "Test error message";
                }
                return Task.CompletedTask;
            };
        }, new ExecutionResult { Errors = new ExecutionErrors { expectedError }, Executed = true });
    }

    public class DocListener : DocumentExecutionListenerBase
    {
        public override Task BeforeExecutionAsync(IExecutionContext context) => throw new ApplicationException2();
    }

    public class ApplicationException2 : Exception { }

    public class Query
    {
        public int Hello()
        {
            throw new Exception("arrgh");
        }

        public int Hello2()
        {
            throw new ApplicationException("error!");
        }
    }
}
