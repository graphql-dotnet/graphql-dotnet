using System.Collections.Generic;

namespace GraphQL.Language
{
    public class InlineFragment : AbstractNode, IFragment
    {
        public NamedType Type { get; set; }

        public Directives Directives { get; set; }

        public Selections Selections { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                foreach (var directive in Directives)
                {
                    yield return directive;
                }

                foreach (var selection in Selections)
                {
                    yield return selection;
                }
            }
        }

        public override string ToString()
        {
            return "InlineFragment{{typeCondition={0}, directives={1}, selections={2}}}"
                .ToFormat(Type, Directives, Selections);
        }

        protected bool Equals(InlineFragment other)
        {
            return Equals(Type, other.Type);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InlineFragment) obj);
        }
    }
}
