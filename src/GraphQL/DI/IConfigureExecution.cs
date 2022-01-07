using System;
using System.Threading.Tasks;

namespace GraphQL.DI
{
    /// <summary>
    /// Allows configuration of execution options immediately prior to executing a document.
    /// </summary>
    public interface IConfigureExecution
    {
        /// <summary>
        /// Configures execution options immediately prior to executing a document.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used to resolve other services from the dependency injection framework.
        /// </remarks>
        Task ConfigureAsync(ExecutionOptions executionOptions);
    }

    internal class ConfigureExecution : IConfigureExecution
    {
        private readonly Action<ExecutionOptions> _action;

        public ConfigureExecution(Action<ExecutionOptions> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public Task ConfigureAsync(ExecutionOptions executionOptions)
        {
            _action(executionOptions);
            return Task.CompletedTask;
        }
    }

    internal class ConfigureExecutionAsync : IConfigureExecution
    {
        private readonly Func<ExecutionOptions, Task> _action;

        public ConfigureExecutionAsync(Func<ExecutionOptions, Task> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public Task ConfigureAsync(ExecutionOptions executionOptions)
        {
            return _action(executionOptions);
        }
    }
}
