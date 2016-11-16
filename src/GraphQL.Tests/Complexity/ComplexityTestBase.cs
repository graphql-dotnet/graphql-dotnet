using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.Tests.StarWars;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityTestBase
    {
        // For our heuristics in these tests it is assumed that each Field returns on average of two results.
        public ComplexityAnalyzer Analyzer { get; } = new ComplexityAnalyzer(50);
        public IDocumentBuilder DocumentBuilder { get; } = new GraphQLDocumentBuilder();
        public StarWarsTestBase StarWarsTestBase { get; } = new StarWarsBasicQueryTests();

        protected ComplexityResult AnalyzeComplexity(string query) => Analyzer.Analyze(DocumentBuilder.Build(query));

        public async Task<ExecutionResult> Execute(ComplexityConfiguration complexityConfig, string query) =>
            await StarWarsTestBase.Executer.ExecuteAsync(options =>
            {
                options.Schema = new StarWarsTestBase().Schema;
                options.Query = query;
                options.ComplexityConfiguration = complexityConfig;
            });
    }
}
