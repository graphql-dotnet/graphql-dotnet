using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conversion;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution.Performance
{
    public class ThreadPerformanceTests : QueryTestBase<ThreadPerformanceTests.ThreadPerformanceSchema>
    {
        public ThreadPerformanceTests()
        {
            Services.Register<PerfQuery>();

            Services.Singleton(() => new ThreadPerformanceSchema(new FuncDependencyResolver(type => (GraphType) Services.Get(type))));
        }

        public class PerfQuery : ObjectGraphType<object>
        {
            public PerfQuery()
            {
                Name = "Query";

                FieldAsync<StringGraphType, string>("halfSecond", resolve: c => Get(500, "Half"));
                FieldAsync<StringGraphType, string>("quarterSecond", resolve: c => Get(500, "Quarter"));
            }

            private async Task<string> Get(int milliseconds, string result)
            {
                await Task.Delay(milliseconds);
                return result;
            }
        }

        public class PerfMutation : ObjectGraphType<object>
        {
            public static readonly List<string> Calls = new List<string>();

            public PerfMutation()
            {
                Name = "Mutation";

                FieldAsync<StringGraphType, string>("setFive", resolve: c => Set("5"));
                FieldAsync<StringGraphType, string>("setOne", resolve: c => Set("1"));
            }

            private Task<string> Set(string result)
            {
                Calls.Add(result);
                var list = string.Join(",", Calls.ToList());
                return Task.FromResult(list);
            }
        }

        public class ThreadPerformanceSchema : Schema
        {
            public ThreadPerformanceSchema(IDependencyResolver resolver)
                : base(resolver)
            {
                Query = resolver.Resolve<PerfQuery>();
                Mutation = resolver.Resolve<PerfMutation>();
            }
        }

        [Fact(Skip = "May fail on a single processor machine.")]
        // [Fact]
        public void Executes_IsQuickerThanTotalTaskTime()
        {
            var query = @"
                query HeroNameAndFriendsQuery {
                  halfSecond,
                  quarterSecond
                }
            ";

            var smallListTimer = new Stopwatch();
            smallListTimer.Start();

            var runResult2 = Executer.ExecuteAsync(_ =>
            {
                _.EnableMetrics = false;
                _.SetFieldMiddleware = false;
                _.Schema = Schema;
                _.Query = query;
            }).GetAwaiter().GetResult();

            smallListTimer.Stop();

            runResult2.Errors.ShouldBeNull();
            smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(900);
        }

        [Fact]
        public async Task Mutations_RunSyncronously()
        {
            var query = @"
                mutation Multiple {
                  m1:setFive
                  m2:setFive
                  m3:setOne
                  m4:setOne
                  m5:setOne
                  m6:setFive
                  m7:setFive
                  m8:setFive
                  m9:setFive
                  m10:setOne
                  m11:setFive
                  m12:setOne
                  m13:setFive
                  m14:setOne
                  m15:setFive
                  m16:setOne
                  m17:setFive
                }
            ";

            var runResult2 = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
            });

            var result = runResult2.Data as dynamic;
            runResult2.Errors.ShouldBeNull();
            ((string)result["m17"]).ShouldBe("5,5,1,1,1,5,5,5,5,1,5,1,5,1,5,1,5");
        }
    }
}
