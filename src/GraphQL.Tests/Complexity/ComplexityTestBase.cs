using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Tests.StarWars;
using GraphQL.Validation.Complexity;

namespace GraphQL.Tests.Complexity
{
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
                options.Schema = new StarWarsTestBase().Schema;
                options.Query = query;
                options.ComplexityConfiguration = complexityConfig;
            });
    }
}
