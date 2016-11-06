using GraphQL.Language.AST;

namespace GraphQL.Validation.Complexity
{
    public interface IComplexityAnalyzer
    {
        ComplexityResult Analyze(Document doc, double avgImpact = 2);
    }
}