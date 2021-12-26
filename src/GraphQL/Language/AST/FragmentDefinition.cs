using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a fragment definition node within a document.
    /// </summary>
    public class FragmentDefinition : AbstractNode, IDefinition, IHaveSelectionSet
    {
        /// <summary>
        /// Initializes a new fragment definition node with the specified <see cref="NameNode"/> containing the name of this fragment definition and its selection set.
        /// </summary>
        public FragmentDefinition(NameNode node, NamedType type, SelectionSet selectionSet)
        {
            NameNode = node;
            Type = type;
            SelectionSet = selectionSet;
        }

        /// <summary>
        /// Returns the name of this fragment definition.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of this fragment definition.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Gets or sets the type node representing the graph type of this fragment definition.
        /// </summary>
        public NamedType Type { get; }

        /// <summary>
        /// Gets or sets a list of directives applied to this fragment definition node.
        /// </summary>
        public Directives? Directives { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet { get; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (Directives != null)
                    yield return Directives;

                yield return SelectionSet;
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            action(Type, state);
            if (Directives != null)
                action(Directives, state);
            action(SelectionSet, state);
        }

        /// <inheritdoc/>
        public override string ToString() => $"FragmentDefinition{{name='{Name}', typeCondition={Type}, directives={Directives}, selectionSet={SelectionSet}}}";
    }
}
