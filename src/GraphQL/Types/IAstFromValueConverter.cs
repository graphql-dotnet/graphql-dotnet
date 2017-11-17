
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public interface IAstFromValueConverter
    {
        bool Matches(object value, IGraphType type);
        IValue Convert(object value, IGraphType type);
    }
}
