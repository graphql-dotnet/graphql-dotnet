using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node within a document.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Returns the node's location within the source document.
        /// </summary>
        SourceLocation SourceLocation { get; }

        /// <summary>
        /// Returns a list of children nodes. If the node doesn't have children, returns <see langword="null"/>.
        /// </summary>
        IEnumerable<INode> Children { get; }

        /// <summary>
        /// Visits every child node with the specified delegate and state. If the node doesn't have children, does nothing.
        /// </summary>
        /// <typeparam name="TState">Type of the provided state.</typeparam>
        /// <param name="action">Delegate to execute on every child node of this node.</param>
        /// <param name="state">An arbitrary state passed by the caller.</param>
        void Visit<TState>(Action<INode, TState> action, TState state);
    }
}
