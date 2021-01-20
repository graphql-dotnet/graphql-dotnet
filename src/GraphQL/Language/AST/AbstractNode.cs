using System;
using System.Collections.Generic;
using GraphQLParser;

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
        public ROM Comment => CommentNode?.Value ?? default;

        /// <summary>
        /// Returns the comment node associated with the node.
        /// </summary>
        public CommentNode CommentNode { get; set; }

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; set; }

        /// <inheritdoc/>
        public virtual IEnumerable<INode> Children => null;

        /// <inheritdoc/>
        public virtual void Visit<TState>(Action<INode, TState> action, TState state) { }
    }
}
