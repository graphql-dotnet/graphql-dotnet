using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public interface IComplexityAnalyzer
    {
        ComplexityResult Analyze(Document doc);
    }
}