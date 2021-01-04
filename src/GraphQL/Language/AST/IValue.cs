namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node within a document.
    /// </summary>
    public interface IValue : INode
    {
        /// <summary>
        /// Returns the value of the node.
        /// </summary>
        object Value { get; }
    }

    /// <inheritdoc cref="IValue"/>
    public interface IValue<T> : IValue
    {
        /// <inheritdoc cref="IValue.Value"/>
        new T Value { get; }
    }
}
