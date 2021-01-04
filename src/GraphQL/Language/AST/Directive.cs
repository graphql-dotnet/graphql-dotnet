using System;
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
        public NameNode NameNode { get; set; }

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

        /// <summary>
        /// Compares this instance with another instance by name.
        /// </summary>
        protected bool Equals(Directive other)
        {
            if (other == null)
                return false;

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
            return Equals((Directive)obj);
        }
    }
}
