using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a fragment spread node within a document.
    /// </summary>
    public class FragmentSpread : AbstractNode, IFragment, IHaveName
    {
        /// <summary>
        /// Initializes a new instance with the specified <see cref="NameNode"/> containing the name of this fragment spread node.
        /// </summary>
        public FragmentSpread(NameNode node)
        {
            NameNode = node;
        }

        /// <summary>
        /// Returns the name of this fragment spread.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of this fragment spread.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Gets or sets a list of directive nodes that apply to this fragment spread node.
        /// </summary>
        public Directives Directives { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => Directives;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state) => action(Directives, state);

        /// <inheritdoc/>
        public override string ToString() => $"FragmentSpread{{name='{Name}', directives={Directives}}}";
    }
}
