using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a named type node within a document.
    /// </summary>
    public class NamedType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a new named type node containing the specified <see cref="NameNode"/>.
        /// </summary>
        /// <param name="node"></param>
        public NamedType(NameNode node)
        {
            NameNode = node;
        }

        /// <summary>
        /// Returns the name of the named type node.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of the type.
        /// </summary>
        public NameNode NameNode { get; }

        /// <inheritdoc/>
        public override string ToString() => $"NamedType{{name={Name}}}";

        /// <summary>
        /// Compares this instance to another instance by comparing the name of the type.
        /// </summary>
        protected bool Equals(NamedType other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((NamedType)obj);
        }
    }
}
