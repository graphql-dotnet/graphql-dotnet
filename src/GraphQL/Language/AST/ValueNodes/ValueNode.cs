namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node which contains a literal value within a document.
    /// </summary>
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected ValueNode()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        protected ValueNode(T value)
        {
            Value = value;
        }

        /// <inheritdoc/>
        public T Value { get; protected set; }

        object IValue.Value => Value;

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";
    }
}
