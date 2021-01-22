using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Tests.Introspection;
using GraphQL.Tests.StarWars;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class MultithreadedTests
    {
        [Fact]
        public async Task test()
        {
            // create a middleware that increments a variable when every field is resolved
            var middleware = new FieldMiddlewareBuilder();
            int count = 0;
            middleware.Use((schema, d) => context => {
                Interlocked.Increment(ref count);
                return d(context);
            });

            // prep a test execution using that middleware
            StarWarsTestBase starWarsTest = null;
            Func<Task> testExecution = async () =>
            {
                var result = await starWarsTest.Executer.ExecuteAsync(new ExecutionOptions
                {
                    Query = SchemaIntrospection.IntrospectionQuery,
                    Schema = starWarsTest.Schema,
                    FieldMiddleware = middleware,
                });
                result.Errors.ShouldBeNull();
            };

            // run a single execution and record the number of times the resolver executed
            starWarsTest = new StarWarsTestBase();
            await testExecution();
            var correctCount = count;

            // test initializing the schema first, followed by 3 simultaneous executions
            starWarsTest = new StarWarsTestBase();
            await testExecution();
            count = 0;
            var t1 = Task.Run(testExecution);
            var t2 = Task.Run(testExecution);
            var t3 = Task.Run(testExecution);
            await Task.WhenAll(t1, t2, t3);
            count.ShouldBe(correctCount * 3, "Failed synchronized initialization");

            // test three simultaneous executions on an uninitialized schema
            count = 0;
            starWarsTest = new StarWarsTestBase();
            t1 = Task.Run(testExecution);
            t2 = Task.Run(testExecution);
            t3 = Task.Run(testExecution);
            await Task.WhenAll(t1, t2, t3);
            count.ShouldBe(correctCount * 3, "Failed multithreaded initialization");
        }
    }
}
