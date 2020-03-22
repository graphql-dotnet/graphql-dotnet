using System;

namespace GraphQL.Language.AST
{
    public class CommentNode : AbstractNode
    {
        public CommentNode(string comment)
        {
            Comment = comment;
        }

        public string Comment { get; }

        protected bool Equals(CommentNode other) => string.Equals(Comment, other.Comment, StringComparison.InvariantCulture);

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((CommentNode)obj);
        }
    }
}
