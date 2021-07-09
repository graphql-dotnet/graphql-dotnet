#nullable enable

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
    }
}
