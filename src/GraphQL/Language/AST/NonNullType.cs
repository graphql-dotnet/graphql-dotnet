using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a non-null type node within a document.
    /// </summary>
    public class NonNullType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a new instance that wraps the specified type node.
        /// </summary>
        public NonNullType(IType type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the wrapped type node.
        /// </summary>
        public IType Type { get; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get { yield return Type; }
        }

        /// <inheritdoc/>
        public override string ToString() => $"NonNullType{{type={Type}}}";

        /// <inheritdoc/>
        public override bool IsEqualTo(INode node)
        {
            if (node is null)
                return false;
            if (ReferenceEquals(this, node))
                return true;
            if (node.GetType() != GetType())
                return false;

            return true;
        }
    }
}
