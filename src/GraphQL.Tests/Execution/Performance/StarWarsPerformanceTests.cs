using System.Diagnostics;
using GraphQL.Tests.StarWars;
using Xunit.Abstractions;

namespace GraphQL.Tests.Execution.Performance;

public class StarWarsPerformanceTests : StarWarsTestBase
{
    private readonly ITestOutputHelper _output;

    public StarWarsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Benchmarks only, these numbers are machine dependent.")]
    // [Fact]
    public void Executes_StarWarsBasicQuery_Performant()
    {
        var query = @"
                query HeroNameAndFriendsQuery {
                  hero {
                    id
                    name
                    appearsIn
                    friends {
                      name
                      appearsIn
                    }
                  }
                }
            ";

        var smallListTimer = new Stopwatch();
        ExecutionResult runResult2 = null;
        smallListTimer.Start();

        //Note: Implementing a custom IDocumentValidator would increase speeds 600%
        for (var x = 0; x < 10000; x++)
        {
            runResult2 = Executer.ExecuteAsync(_ =>
            {
                _.EnableMetrics = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = null;
                _.Variables = null;
                _.UserContext = null;
                _.CancellationToken = default;
                _.ValidationRules = null;
            }).GetAwaiter().GetResult();
        }

        smallListTimer.Stop();

        _output.WriteLine($"Milliseconds: {smallListTimer.ElapsedMilliseconds}");

        runResult2.Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(9400 * 2); //machine specific data with a buffer
    }
}
