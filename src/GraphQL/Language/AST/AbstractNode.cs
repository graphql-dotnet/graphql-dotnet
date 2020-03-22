using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public abstract class AbstractNode : INode
    {
        public string Comment => CommentNode?.Comment;
        public CommentNode CommentNode { get; set; }

        public virtual IEnumerable<INode> Children => null;

        public SourceLocation SourceLocation { get; set; }

        public abstract bool IsEqualTo(INode node);
    }
}
