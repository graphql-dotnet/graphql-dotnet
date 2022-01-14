namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node within a document.
    /// </summary>
    public interface IValue
    {
        /// <summary>
        /// Returns the value of the node.
        /// </summary>
        object? ClrValue { get; }
    }

    /// <inheritdoc cref="IValue"/>
    public interface IValue<T> : IValue
    {
        /// <inheritdoc cref="IValue.ClrValue"/>
        new T ClrValue { get; }
    }
}
