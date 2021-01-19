namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node which contains a literal value within a document.
    /// </summary>
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        /// <inheritdoc/>
        public T Value { get; protected set; }

        object IValue.Value => Value;

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";
    }
}
