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

        /// <inheritdoc />
        public override string ToString() => $"InlineFragment{{typeCondition={Type}, directives={Directives}, selections={SelectionSet}}}";

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals(Type, ((InlineFragment)obj).Type);
        }
    }
}
