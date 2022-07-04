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
        Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next);
    }

    /// <summary>
    /// A function that can process a GraphQL document.
    /// </summary>
    public delegate Task<ExecutionResult> ExecutionDelegate(ExecutionOptions options);

    internal class ConfigureExecution : IConfigureExecution
    {
        private readonly Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> _action;

        public ConfigureExecution(Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> action)
        {
            _action = action;
        }

        public Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
            => _action(options, next);
    }
}
