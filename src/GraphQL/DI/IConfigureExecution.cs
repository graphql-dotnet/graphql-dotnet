namespace GraphQL.DI
{
    /// <summary>
    /// Allows configuration of document execution, adding or replacing default behavior.
    /// This configuration generally happens in <see cref="IDocumentExecuter.ExecuteAsync" /> implementations.
    /// </summary>
    public interface IConfigureExecution
    {
        /// <summary>
        /// Called when the document begins executing, passing in a delegate to continue execution.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used to resolve other services from the dependency injection framework.
        /// </remarks>
        Task<ExecutionResult> ExecuteAsync(Func<ExecutionOptions, Task<ExecutionResult>> next, ExecutionOptions executionOptions);
    }

    internal class ConfigureExecution : IConfigureExecution
    {
        private readonly Func<Func<ExecutionOptions, Task<ExecutionResult>>, ExecutionOptions, Task<ExecutionResult>> _action;

        public ConfigureExecution(Func<Func<ExecutionOptions, Task<ExecutionResult>>, ExecutionOptions, Task<ExecutionResult>> action)
        {
            _action = action;
        }

        public Task<ExecutionResult> ExecuteAsync(Func<ExecutionOptions, Task<ExecutionResult>> next, ExecutionOptions executionOptions)
            => _action(next, executionOptions);
    }
}
