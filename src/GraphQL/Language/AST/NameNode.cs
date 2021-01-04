using System;
using System.Collections.Generic;

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
        public NameNode(string name, SourceLocation location)
        {
            Name = name;
            SourceLocation = location;
        }

        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(string name)
        {
            Name = name;
            SourceLocation = null;
        }

        /// <summary>
        /// Returns the contained name.
        /// </summary>
        public string Name { get; }

        IEnumerable<INode> INode.Children => throw new NotImplementedException();

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; }

        /// <inheritdoc/>
        bool INode.IsEqualTo(INode node) => throw new NotImplementedException();
    }
}
