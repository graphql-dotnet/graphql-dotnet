using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.Tests.StarWars;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Complexity;

public class LegacyComplexityTestBase
{
    // For our heuristics in these tests it is assumed that each Field returns on average of two results.
    public IDocumentBuilder DocumentBuilder { get; } = new GraphQLDocumentBuilder { MaxDepth = 1000 };

    public StarWarsTestBase StarWarsTestBase { get; } = new StarWarsBasicQueryTests();

    protected LegacyComplexityResult AnalyzeComplexity(string query) => LegacyComplexityValidationRule.Analyze(DocumentBuilder.Build(query), 2.0d, 250);

    public async Task<ExecutionResult> Execute(LegacyComplexityConfiguration complexityConfig, string query, bool onlyComplexityRule = false) =>
        await StarWarsTestBase.Executer.ExecuteAsync(options =>
        {
            options.Schema = CreateSchema();
            options.Query = query;
            options.ValidationRules = onlyComplexityRule
                ? new[] { new LegacyComplexityValidationRule(complexityConfig) }
                : GraphQL.Validation.DocumentValidator.CoreRules.Append(new LegacyComplexityValidationRule(complexityConfig));
        }).ConfigureAwait(false);

    //ISSUE: manually created test instance with ServiceProvider
    private static StarWarsSchema CreateSchema()
    {
        var builder = new MicrosoftDI.GraphQLBuilder(new ServiceCollection(), b => new StarWarsTestBase().RegisterServices(b.Services));
        return builder.ServiceCollection.BuildServiceProvider().GetRequiredService<StarWarsSchema>();
    }
}
