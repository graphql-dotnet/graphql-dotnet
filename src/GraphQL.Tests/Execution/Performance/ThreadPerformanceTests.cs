using System.Diagnostics;
using GraphQL.DI;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Execution.Performance;

public class ThreadPerformanceTests : QueryTestBase<ThreadPerformanceTests.ThreadPerformanceSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        register.Transient<PerfQuery>();
        register.Transient<PerfMutation>();
        register.Singleton<ThreadPerformanceSchema>();
    }

    public class PerfQuery : ObjectGraphType<object>
    {
        public PerfQuery()
        {
            Name = "Query";

            Field<StringGraphType, string>("halfSecond").ResolveAsync(_ => Get(500, "Half"));
            Field<StringGraphType, string>("quarterSecond").ResolveAsync(_ => Get(500, "Quarter"));
        }

        private async Task<string> Get(int milliseconds, string result)
        {
            await Task.Delay(milliseconds).ConfigureAwait(false);
            return result;
        }
    }

    public class PerfMutation : ObjectGraphType<object>
    {
        public static readonly List<string> Calls = new();

        public PerfMutation()
        {
            Name = "Mutation";

            Field<StringGraphType, string>("setFive").ResolveAsync(_ => Set("5"));
            Field<StringGraphType, string>("setOne").ResolveAsync(_ => Set("1"));
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
        public ThreadPerformanceSchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<PerfQuery>();
            Mutation = serviceProvider.GetRequiredService<PerfMutation>();
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
            _.Schema = Schema;
            _.Query = query;
        }).GetAwaiter().GetResult();

        smallListTimer.Stop();

        runResult2.Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(900);
    }

    [Fact]
    public async Task Mutations_RunSynchronously()
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

        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = query;
        }).ConfigureAwait(false);

        result.Errors.ShouldBeNull();
        ((string)result.Data.ToDict()["m17"]).ShouldBe("5,5,1,1,1,5,5,5,5,1,5,1,5,1,5,1,5");
    }
}
