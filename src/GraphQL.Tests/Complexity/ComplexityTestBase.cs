using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.Tests.StarWars;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Complexity;

public class ComplexityTestBase
{
    // For our heuristics in these tests it is assumed that each Field returns on average of two results.
    public ComplexityAnalyzer Analyzer { get; } = new ComplexityAnalyzer();

    public IDocumentBuilder DocumentBuilder { get; } = new GraphQLDocumentBuilder { MaxDepth = 1000 };

    public StarWarsTestBase StarWarsTestBase { get; } = new StarWarsBasicQueryTests();

    protected ComplexityResult AnalyzeComplexity(string query) => Analyzer.Analyze(DocumentBuilder.Build(query), 2.0d, 250);

    public async Task<ExecutionResult> Execute(ComplexityConfiguration complexityConfig, string query) =>
        await StarWarsTestBase.Executer.ExecuteAsync(options =>
        {
            options.Schema = CreateSchema();
            options.Query = query;
            options.ComplexityConfiguration = complexityConfig;
        }).ConfigureAwait(false);

    //ISSUE: manually created test instance with ServiceProvider
    private static StarWarsSchema CreateSchema()
    {
        var builder = new MicrosoftDI.GraphQLBuilder(new ServiceCollection(), b => new StarWarsTestBase().RegisterServices(b.Services));
        return builder.ServiceCollection.BuildServiceProvider().GetRequiredService<StarWarsSchema>();
    }
}
