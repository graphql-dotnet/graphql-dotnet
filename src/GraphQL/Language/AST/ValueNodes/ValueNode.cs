namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node which contains a literal value within a document.
    /// </summary>
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        public ValueNode(T value)
        {
            Value = value;
        }

        /// <inheritdoc/>
        public T Value { get; }

        object IValue.Value => Value!;

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";
    }
}
