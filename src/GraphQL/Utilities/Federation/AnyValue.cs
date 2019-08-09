using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Utilities.Federation
{
    public class AnyValue : IValue
    {
        public AnyValue(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public IEnumerable<INode> Children => Enumerable.Empty<INode>();

        public SourceLocation SourceLocation { get; }

        public bool IsEqualTo(INode node)
        {
            return false;
        }
    }
}
