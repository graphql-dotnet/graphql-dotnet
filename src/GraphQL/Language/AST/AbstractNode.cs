using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public abstract class AbstractNode : INode
    {
        public string Comment => CommentNode?.Value;

        public CommentNode CommentNode { get; set; }

        public virtual IEnumerable<INode> Children => null;

        public SourceLocation SourceLocation { get; set; }

        public abstract bool IsEqualTo(INode node);
    }
}
