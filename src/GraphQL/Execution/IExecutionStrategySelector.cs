namespace GraphQL.Execution
{
    /// <summary>
    /// Returns an instance of an <see cref="IExecutionStrategy"/> for a specified <see cref="ExecutionContext"/>.
    /// </summary>
    public interface IExecutionStrategySelector
    {
        /// <inheritdoc cref="IExecutionStrategySelector"/>
        IExecutionStrategy Select(ExecutionContext context);
    }
}
