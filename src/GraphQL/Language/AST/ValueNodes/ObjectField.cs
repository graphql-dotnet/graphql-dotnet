using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the child field node of an object value node within a document.
    /// </summary>
    public class ObjectField : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance for the specified field name and value.
        /// </summary>
        public ObjectField(string name, IValue value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Returns the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the value node containing the value of the field.
        /// </summary>
        public IValue Value { get; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        /// <inheritdoc/>
        public override string ToString() => $"ObjectField{{name='{Name}', value={Value}}}";
    }
}
