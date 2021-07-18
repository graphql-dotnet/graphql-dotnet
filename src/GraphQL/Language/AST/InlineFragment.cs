using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an inline fragment node within a document.
    /// </summary>
    public class InlineFragment : AbstractNode, IFragment, IHaveSelectionSet
    {
        [Obsolete]
        public InlineFragment()
        {
            SelectionSet = null!;
        }

        public InlineFragment(SelectionSet selectionSet)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            SelectionSet = selectionSet;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Gets or sets the named type node of this fragment.
        /// </summary>
        public NamedType? Type { get; set; }

        /// <summary>
        /// Gets or set a list of directives that apply to this fragment.
        /// </summary>
        public Directives? Directives { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet
        {
            get;
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            [Obsolete]
            set;
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                if (Type != null)
                    yield return Type;

                if (Directives != null)
                    yield return Directives;

                if (SelectionSet != null)
                    yield return SelectionSet;
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (Type != null)
                action(Type, state);
            if (Directives != null)
                action(Directives, state);
            action(SelectionSet, state);
        }

        /// <inheritdoc/>
        public override string ToString() => $"InlineFragment{{typeCondition={Type}, directives={Directives}, selections={SelectionSet}}}";
    }
}
