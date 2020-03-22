using System;

namespace GraphQL.Language.AST
{
    public class CommentNode : AbstractNode
    {
        protected string _comment;

        public CommentNode(string comment)
        {
            _comment = comment;
        }

        public new string Comment => _comment;

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
