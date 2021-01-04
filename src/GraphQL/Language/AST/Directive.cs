using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a directive node within a document.
    /// </summary>
    public class Directive : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance of a directive node with the specified parameters.
        /// </summary>
        public Directive(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the name of this directive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the node containing a list of argument nodes for this directive.
        /// </summary>
        public Arguments Arguments { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get { yield return Arguments; }
        }

        /// <inheritdoc />
        public override string ToString() => $"Directive{{name='{Name}',arguments={Arguments}}}";
    }
}
