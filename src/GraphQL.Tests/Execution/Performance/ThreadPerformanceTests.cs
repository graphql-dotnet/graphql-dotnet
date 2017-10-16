using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution.Performance
{
    public class ThreadPerformanceTests : QueryTestBase<ThreadPerformanceTests.ThreadPerformanceSchema>
    {
        public ThreadPerformanceTests()
        {
            Services.Register<PerfQuery>();

            Services.Singleton(() => new ThreadPerformanceSchema(type => (GraphType) Services.Get(type)));
        }

        public class PerfQuery : ObjectGraphType<object>
        {
            public PerfQuery()
            {
                Name = "Query";

                FieldAsync<StringGraphType>("halfSecond", resolve: c => Get(500, "Half"));
                FieldAsync<StringGraphType>("quarterSecond", resolve: async c => Get(500, "Quarter"));
            }

            private string Get(int milliseconds, string result)
            {
                Thread.Sleep(milliseconds);

                return result;
            }
        }

        public class PerfMutation : ObjectGraphType<object>
        {
            public static readonly List<string> Calls = new List<string>();

            public PerfMutation()
            {
                Name = "Mutation";

                FieldAsync<StringGraphType>("setFive", resolve: c => Set("5"));
                FieldAsync<StringGraphType>("setOne", resolve: async c => Set("1"));
            }

            private string Set(string result)
            {
                Calls.Add(result);
                return string.Join(",", Calls.ToList());
            }
        }

        public class ThreadPerformanceSchema : Schema
        {
            public ThreadPerformanceSchema(Func<Type, GraphType> resolveType)
                : base(resolveType)
            {
                Query = (PerfQuery) resolveType(typeof(PerfQuery));
                Mutation = (PerfMutation) resolveType(typeof(PerfMutation));
            }
        }

        [Fact(Skip = "May fail one a single processor machine.")]
        public void Executes_IsQuickerThanTotalTaskTime()
        {
            var query = @"
                query HeroNameAndFriendsQuery {
                  halfSecond,
                  quarterSecond
                }
            ";

            var smallListTimer = new Stopwatch();
            ExecutionResult runResult2 = null;
            smallListTimer.Start();

            runResult2 = Executer.ExecuteAsync(_ =>
            {
                _.EnableMetrics = false;
                _.SetFieldMiddleware = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = null;
                _.Inputs = null;
                _.UserContext = null;
                _.CancellationToken = default(CancellationToken);
                _.ValidationRules = null;
                _.FieldNameConverter = new CamelCaseFieldNameConverter();
            }).GetAwaiter().GetResult();

            smallListTimer.Stop();

            Assert.Null(runResult2.Errors);

            Assert.True(smallListTimer.ElapsedMilliseconds < 900);
        }

        [Fact]
        public void Mutations_RunSyncronously()
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

            ExecutionResult runResult2 = null;

            runResult2 = Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
                _.Root = null;
                _.Inputs = null;
                _.UserContext = null;
                _.CancellationToken = default(CancellationToken);
                _.ValidationRules = null;
                _.FieldNameConverter = new CamelCaseFieldNameConverter();
            }).GetAwaiter().GetResult();

            var result = runResult2.Data as dynamic;
            Assert.Null(runResult2.Errors);
            Assert.Equal("5,5,1,1,1,5,5,5,5,1,5,1,5,1,5,1,5", result["m17"]);
        }
    }
}
