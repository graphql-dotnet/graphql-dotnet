using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class ExecutionListenerTests : BasicQueryTestBase
    {
        [Fact]
        public void BeforeExecutionAwaited_Called_Correctly()
        {
            var schema = new Schema { Query = new AsyncGraphType() };

            var userContext = new TestContext();

            var breaker = new Timer(_ => userContext.Complete("timeout"), null, 5000, 5000);

            AssertQuerySuccess(opts =>
            {
                opts.Schema = schema;
                opts.Query = "{ foo }";
                opts.UserContext = userContext;
                opts.Listeners.Add(new TestExecutionListener());
            }, @"{ ""foo"": ""bar"" }");

            breaker.Dispose();
        }

        public class AsyncGraphType : ObjectGraphType
        {
            public AsyncGraphType()
            {
                Name = "Query";
                Field<StringGraphType>("foo", resolve: context =>
                {
                    var uc = context.UserContext as TestContext;
                    return uc.ResolveAsync();
                });
            }
        }

        public class TestExecutionListener : DocumentExecutionListenerBase
        {
            [Obsolete]
            public override Task BeforeExecutionAwaitedAsync(IExecutionContext context)
            {
                var testContext = context.UserContext as TestContext;
                testContext.Complete("bar");

                return Task.CompletedTask;
            }
        }

        public class TestContext : Dictionary<string, object>
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
