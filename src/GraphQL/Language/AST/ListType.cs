using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class ListType : AbstractNode, IType
    {
        public ListType(IType type)
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
            return "ListType{{type={0}}}".ToFormat(Type);
        }

        public override bool IsEqualTo(INode node)
        {
            if (node is null) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != GetType()) return false;

            return true;
        }
    }
}
