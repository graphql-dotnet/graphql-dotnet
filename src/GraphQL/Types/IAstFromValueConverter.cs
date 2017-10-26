
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public interface IAstFromValueConverter
    {
        bool Matches(object value);
        IValue Convert(object value);
    }
}
