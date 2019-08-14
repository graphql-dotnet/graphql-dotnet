using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Directive : AbstractNode
    {
        public Directive(NameNode node)
        {
            NameNode = node;
        }

        public string Name => NameNode.Name;
        public NameNode NameNode { get; set; }

        public Arguments Arguments { get; set; }

        public override IEnumerable<INode> Children
        {
            get { yield return Arguments; }
        }

        public override string ToString()
        {
            return $"Directive{{name='{Name}',arguments={Arguments}}}";
        }

        protected bool Equals(Directive other)
        {
            if (other == null)
                return false;

            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Directive)obj);
        }
    }
}
