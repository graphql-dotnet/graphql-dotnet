using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an argument node within a document.
    /// </summary>
    public class Argument : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance of an argument node with the specified properties.
        /// </summary>
        public Argument(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the name of this argument.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the value node for this argument.
        /// </summary>
        public IValue Value { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        /// <inheritdoc />
        public override string ToString() => $"Argument{{name={Name},value={Value}}}";
    }
}
