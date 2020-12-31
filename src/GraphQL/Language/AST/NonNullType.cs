using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class NonNullType : AbstractNode, IType
    {
        public NonNullType(IType type)
        {
            Type = type;
        }

        public IType Type { get; }

        public override IEnumerable<INode> Children
        {
            get { yield return Type; }
        }

        /// <inheritdoc />
        public override string ToString() => $"NonNullType{{type={Type}}}";

        public override bool IsEqualTo(INode node)
        {
            if (node is null) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != GetType()) return false;

            return true;
        }
    }
}
