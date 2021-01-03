using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a comment node within a document.
    /// </summary>
    public class CommentNode : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance with the specified comment value.
        /// </summary>
        public CommentNode(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns the comment stored in this node.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Compares this instance to another <see cref="CommentNode"/> by comment value.
        /// </summary>
        protected bool Equals(CommentNode other) => string.Equals(Value, other.Value, StringComparison.InvariantCulture);

        /// <inheritdoc/>
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
