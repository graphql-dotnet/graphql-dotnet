using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list type node within a document.
    /// </summary>
    public class ListType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a list type node that wraps the specified type node.
        /// </summary>
        public ListType(IType type)
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
        public override string ToString() => $"ListType{{type={Type}}}";

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
