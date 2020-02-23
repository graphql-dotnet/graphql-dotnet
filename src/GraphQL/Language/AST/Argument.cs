using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Argument : AbstractNode
    {
        public Argument()
        {
        }

        public Argument(NameNode name)
        {
            NamedNode = name;
        }

        public string Name => NamedNode?.Name;
        public NameNode NamedNode { get; }
        public IValue Value { get; set; }

        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        public override string ToString()
        {
            return $"Argument{{name={Name},value={Value}}}";
        }

        protected bool Equals(Argument other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Argument)obj);
        }
    }
}
