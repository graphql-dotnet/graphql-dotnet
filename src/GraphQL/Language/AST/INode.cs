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
    }
}
