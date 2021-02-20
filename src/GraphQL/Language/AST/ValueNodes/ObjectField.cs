using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the child field node of an object value node within a document.
    /// </summary>
    public class ObjectField : AbstractNode, IHaveName, IHaveValue
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
        public override void Visit<TState>(Action<INode, TState> action, TState state) => action(Value, state);

        /// <inheritdoc/>
        public override string ToString() => $"ObjectField{{name='{Name}', value={Value}}}";
    }
}
