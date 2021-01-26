using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node within a document.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Returns a list of children nodes. If the node doesn't have children, returns <see langword="null"/>.
        /// </summary>
        IEnumerable<INode> Children { get; }

        /// <summary>
        /// Returns the node's location within the source document.
        /// </summary>
        SourceLocation SourceLocation { get; }

        /// <summary>
        /// Determines if the node is equal to another node.
        /// This typically returns <see langword="true"/> if the node type and the node name matches.
        /// </summary>
        bool IsEqualTo(INode node);
    }
}
