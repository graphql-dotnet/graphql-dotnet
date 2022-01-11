using System;
using System.Collections.Generic;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a name within a document. This could be the name of a field, type, argument, directive, alias, etc.
    /// </summary>
    public readonly struct NameNode : INode
    {
        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(string name, GraphQLLocation location)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SourceLocation = location;
        }

        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SourceLocation = default;
        }

        /// <summary>
        /// Returns the contained name.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc/>
        public GraphQLLocation SourceLocation { get; }

        IEnumerable<INode>? INode.Children => null;

        /// <inheritdoc/>
        public void Visit<TState>(Action<INode, TState> action, TState state) { }
    }
}
