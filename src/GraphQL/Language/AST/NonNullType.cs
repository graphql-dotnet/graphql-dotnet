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

        public override string ToString()
        {
            return "NonNullType{{type={0}}}".ToFormat(Type);
        }

        public override bool IsEqualTo(INode node)
        {
            if (ReferenceEquals(null, node)) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != this.GetType()) return false;

            return true;
        }
    }
}
