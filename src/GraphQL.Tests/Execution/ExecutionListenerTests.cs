using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class ExecutionListenerTests : BasicQueryTestBase
    {
        [Fact]
        public void BeforeExecutionAwaited_Called_Correctly()
        {
            var schema = new Schema();
            schema.Query = new AsyncGraphType();

            var userContext = new TestContext();

            var breaker = new Timer(_ => userContext.Complete("timeout"), null, 5000, 5000);

            AssertQuerySuccess(opts =>
            {
                opts.Schema = schema;
                opts.Query = "{ foo }";
                opts.UserContext = userContext;
                opts.Listeners.Add(new TestExecutionListener());
                opts.ExposeExceptions = true;
            }, @"{ foo: ""bar"" }");

            breaker.Dispose();
        }

        public class AsyncGraphType : ObjectGraphType
        {
            public AsyncGraphType()
            {
                Name = "Query";
                Field<StringGraphType>("foo", resolve: context =>
                {
                    var uc = context.UserContext.As<TestContext>();
                    return uc.ResolveAsync();
                });
            }
        }

        public class TestExecutionListener : DocumentExecutionListenerBase<TestContext>
        {
            public override Task BeforeExecutionAwaitedAsync(TestContext userContext, CancellationToken token)
            {
                userContext.Complete("bar");

                return TaskExtensions.CompletedTask;
            }
        }

        public class TestContext
        {
            private TaskCompletionSource<string> _tcs;

            public void Complete(string result)
            {
                _tcs.TrySetResult(result);
            }

            public Task<string> ResolveAsync()
            {
                _tcs = new TaskCompletionSource<string>();
                return _tcs.Task;
            }
        }
    }
}
