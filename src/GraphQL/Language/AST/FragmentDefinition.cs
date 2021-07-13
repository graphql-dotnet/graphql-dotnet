#nullable enable

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
        /// Initializes a new fragment definition node with the specified <see cref="NameNode"/> containing the name of this fragment definition.
        /// </summary>
        [Obsolete]
        public FragmentDefinition(NameNode node) : this(node, null!, null!)
        {
        }

        /// <summary>
        /// Initializes a new fragment definition node with the specified <see cref="NameNode"/> containing the name of this fragment definition and its selection set.
        /// </summary>
        public FragmentDefinition(NameNode node, NamedType type, SelectionSet selectionSet)
        {
            NameNode = node;
#pragma warning disable CS0612 // Type or member is obsolete
            Type = type;
            SelectionSet = selectionSet;
#pragma warning restore CS0612 // Type or member is obsolete
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
        public NamedType Type { get; [Obsolete] set; }

        /// <summary>
        /// Gets or sets a list of directives applied to this fragment definition node.
        /// </summary>
        public Directives? Directives { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet
        {
            get;
            [Obsolete]
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            set;
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        }

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
