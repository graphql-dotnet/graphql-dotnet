using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the child field node of a object value node within a document.
    /// </summary>
    public class ObjectField : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance for the specified field name and value.
        /// </summary>
        public ObjectField(NameNode name, IValue value)
            : this(name.Name, value)
        {
            NameNode = name;
        }

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
        /// Returns the <see cref="NameNode"/> containing the name of the field, if initialized with the <see cref="ObjectField.ObjectField(NameNode, IValue)"/> constructor.
        /// </summary>
        public NameNode NameNode { get; }

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

        /// <summary>
        /// Compares this instance to another instance by name.
        /// </summary>
        protected bool Equals(ObjectField other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((ObjectField)obj);
        }
    }
}
