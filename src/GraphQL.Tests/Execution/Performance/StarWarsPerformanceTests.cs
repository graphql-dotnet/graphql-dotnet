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
    public async Task Executes_StarWarsBasicQuery_Performant()
    {
        const string query = """
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
            """;

        var smallListTimer = new Stopwatch();
        ExecutionResult? runResult2 = null;
        smallListTimer.Start();

        //Note: Implementing a custom IDocumentValidator would increase speeds 600%
        for (int x = 0; x < 10000; x++)
        {
            runResult2 = await Executer.ExecuteAsync(_ =>
            {
                _.EnableMetrics = false;
                _.Schema = Schema;
                _.Query = query;
                _.Root = null;
                _.Variables = null;
                _.UserContext = null!;
                _.CancellationToken = default;
                _.ValidationRules = null;
            });
        }

        smallListTimer.Stop();

        _output.WriteLine($"Milliseconds: {smallListTimer.ElapsedMilliseconds}");

        runResult2.ShouldNotBeNull().Errors.ShouldBeNull();
        smallListTimer.ElapsedMilliseconds.ShouldBeLessThan(9400 * 2); //machine specific data with a buffer
    }
}
