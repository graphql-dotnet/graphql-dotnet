using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public abstract class AbstractNode : INode
    {
        public virtual IEnumerable<INode> Children => Enumerable.Empty<INode>();

        public SourceLocation SourceLocation { get; set; }

        public abstract bool IsEqualTo(INode node);
    }
}
