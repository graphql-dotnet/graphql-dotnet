using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class InlineFragment : AbstractNode, IFragment, IHaveSelectionSet
    {
        public NamedType Type { get; set; }

        public Directives Directives { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                if (Type != null)
                {
                    yield return Type;
                }

                if (Directives != null)
                {
                    foreach (var directive in Directives)
                    {
                        yield return directive;
                    }
                }

                if (SelectionSet != null)
                {
                    yield return SelectionSet;
                }
            }
        }

        public override string ToString()
        {
            return "InlineFragment{{typeCondition={0}, directives={1}, selections={2}}}"
                .ToFormat(Type, Directives, SelectionSet);
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
