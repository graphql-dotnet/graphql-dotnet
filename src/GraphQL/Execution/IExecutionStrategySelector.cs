namespace GraphQL.Execution
{
    /// <summary>
    /// Returns an instance of an <see cref="IExecutionStrategy"/> for a specified operation type.
    /// </summary>
    public interface IExecutionStrategySelector
    {
        /// <inheritdoc cref="IExecutionStrategySelector"/>
        IExecutionStrategy Select(ExecutionContext context);
    }
}
