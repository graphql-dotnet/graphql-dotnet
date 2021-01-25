using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an inline fragment node within a document.
    /// </summary>
    public class InlineFragment : AbstractNode, IFragment, IHaveSelectionSet
    {
        /// <summary>
        /// Gets or sets the named type node of this fragment.
        /// </summary>
        public NamedType Type { get; set; }

        /// <summary>
        /// Gets or set a list of directives that apply to this fragment.
        /// </summary>
        public Directives Directives { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet { get; set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override string ToString() => $"InlineFragment{{typeCondition={Type}, directives={Directives}, selections={SelectionSet}}}";

        /// <inheritdoc/>
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
