using System;
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
                    yield return Directives;
                }

                if (SelectionSet != null)
                {
                    yield return SelectionSet;
                }
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            action(Type, state);
            action(Directives, state);
            action(SelectionSet, state);
        }

        /// <inheritdoc/>
        public override string ToString() => $"InlineFragment{{typeCondition={Type}, directives={Directives}, selections={SelectionSet}}}";
    }
}
