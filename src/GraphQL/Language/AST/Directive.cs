#nullable enable

using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a directive node within a document.
    /// </summary>
    public class Directive : AbstractNode, IHaveName
    {
        /// <summary>
        /// Initializes a new instance of a directive node with the specified parameters.
        /// </summary>
        public Directive(NameNode node)
        {
            NameNode = node;
        }

        /// <summary>
        /// Returns the name of this directive.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> which contains the name of this directive.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Returns the node containing a list of argument nodes for this directive.
        /// </summary>
        public Arguments? Arguments { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                if (Arguments != null)
                    yield return Arguments;
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (Arguments != null)
                action(Arguments, state);
        }

        /// <inheritdoc />
        public override string ToString() => $"Directive{{name='{Name}',arguments={Arguments}}}";
    }
}
