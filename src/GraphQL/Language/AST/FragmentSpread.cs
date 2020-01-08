using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class FragmentSpread : AbstractNode, IFragment
    {
        public FragmentSpread(NameNode node)
            : this()
        {
            NameNode = node;
        }

        public FragmentSpread()
        {
            Directives = new Directives();
        }

        public string Name => NameNode?.Name;

        public NameNode NameNode { get; }

        public Directives Directives { get; set; }

        public override IEnumerable<INode> Children => Directives;

        public override string ToString()
        {
            return "FragmentSpread{{name='{0}', directives={1}}}".ToFormat(Name, Directives);
        }

        protected bool Equals(FragmentSpread other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FragmentSpread)obj);
        }
    }
}
