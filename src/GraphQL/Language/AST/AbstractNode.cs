using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node within a document.
    /// </summary>
    public abstract class AbstractNode : INode
    {
        /// <summary>
        /// Returns the comment associated with the node.
        /// </summary>
        public string Comment => CommentNode?.Value;

        /// <summary>
        /// Returns the comment node associated with the node.
        /// </summary>
        public CommentNode CommentNode { get; set; }

        /// <inheritdoc/>
        public virtual IEnumerable<INode> Children => null;

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; set; }

        /// <inheritdoc/>
        public abstract bool IsEqualTo(INode node);
    }
}
