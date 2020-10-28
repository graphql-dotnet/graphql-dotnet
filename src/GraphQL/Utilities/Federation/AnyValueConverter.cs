using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class AnyValueConverter : IAstFromValueConverter
    {
        public IValue Convert(object value, IGraphType type) => new AnyValue(value);

        public bool Matches(object value, IGraphType type) => type.Name == "_Any";
    }
}
