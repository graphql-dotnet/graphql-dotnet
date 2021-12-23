using System;
using System.Threading.Tasks;

namespace GraphQL.DI // TODO: think about namespaces!
{
    /// <summary>
    /// Allows configuration of execution options immediately prior to executing a document.
    /// This configuration generally happens in <see cref="IDocumentExecuter.ExecuteAsync" /> implementations.
    /// </summary>
    public interface IConfigureExecutionOptions
    {
        /// <summary>
        /// Configures execution options immediately prior to executing a document.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used to resolve other services from the dependency injection framework.
        /// </remarks>
        Task ConfigureAsync(ExecutionOptions executionOptions);
    }

    internal sealed class ConfigureExecutionOptions : IConfigureExecutionOptions
    {
        private readonly Func<ExecutionOptions, Task> _action;

        public ConfigureExecutionOptions(Func<ExecutionOptions, Task> action)
        {
            _action = action;
        }

        public ConfigureExecutionOptions(Action<ExecutionOptions> action)
        {
            _action = opt => { action(opt); return Task.CompletedTask; };
        }

        public Task ConfigureAsync(ExecutionOptions executionOptions)
        {
            return _action(executionOptions);
        }
    }
}
