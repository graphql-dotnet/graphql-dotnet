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
        public FragmentDefinition(NameNode node)
        {
            NameNode = node;
        }

        /// <summary>
        /// Returns the name of this fragment definition.
        /// </summary>
        public string Name => NameNode?.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of this fragment definition.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Gets or sets the type node representing the graph type of this fragment definition.
        /// </summary>
        public NamedType Type { get; set; }

        /// <summary>
        /// Gets or sets a list of directives applied to this fragment definition node.
        /// </summary>
        public Directives Directives { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (Directives != null)
                {
                    foreach (var directive in Directives)
                    {
                        yield return directive;
                    }
                }

                yield return SelectionSet;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"FragmentDefinition{{name='{Name}', typeCondition={Type}, directives={Directives}, selectionSet={SelectionSet}}}";

        /// <summary>
        /// Compares this instance to another instance by name.
        /// </summary>
        protected bool Equals(FragmentDefinition other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((FragmentDefinition)obj);
        }
    }
}
