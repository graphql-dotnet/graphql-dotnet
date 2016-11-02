using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public interface IComplexityAnalyzer
    {
        ComplexityAnalyzer.ComplexityResult Analyze(Document doc);
    }
}