using System;

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
        void Configure(ExecutionOptions executionOptions);
    }

    internal class ConfigureExecution : IConfigureExecution
    {
        private readonly Action<ExecutionOptions> _action;

        public ConfigureExecution(Action<ExecutionOptions> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Configure(ExecutionOptions executionOptions)
        {
            _action(executionOptions);
        }
    }
}
