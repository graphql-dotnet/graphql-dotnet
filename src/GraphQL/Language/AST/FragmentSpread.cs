using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class FragmentSpread : AbstractNode, IFragment
    {
        public FragmentSpread(NameNode node)
            : this()
        {
            Name = node.Name;
            NameNode = node;
        }

        public FragmentSpread()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }
        public NameNode NameNode { get; }

        public Directives Directives { get; set; }

        public override IEnumerable<INode> Children => Directives;

        public override string ToString()
        {
            return "FragmentSpread{{name='{0}', directives={1}}}".ToFormat(Name, Directives);
        }

        protected bool Equals(FragmentSpread other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FragmentSpread) obj);
        }
    }
}
