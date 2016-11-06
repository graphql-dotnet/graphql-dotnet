using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityTestBase
    {
        // For our heuristics in these tests it is assumed that each Field returns on average of two results.
        public ComplexityAnalyzer Analyzer { get; } = new ComplexityAnalyzer(50);
        public IDocumentBuilder DocumentBuilder { get; } = new GraphQLDocumentBuilder();

        protected ComplexityResult AnalyzeComplexity(string query) => Analyzer.Analyze(DocumentBuilder.Build(query));
    }
}
