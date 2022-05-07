namespace GraphQL.DI // TODO: think about namespaces!
{
    /// <summary>
    /// Allows configuration of execution options immediately prior to executing a document.
    /// This configuration generally happens in <see cref="IDocumentExecuter.ExecuteAsync" /> implementations.
    /// </summary>
    [Obsolete("Use IConfigureExecution; will be removed in v6")]
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

#pragma warning disable CS0618 // Type or member is obsolete
    internal sealed class ConfigureExecutionOptions : IConfigureExecutionOptions // implement IConfigureExecution for v6
#pragma warning restore CS0618 // Type or member is obsolete
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

    /// <summary>
    /// Maps old <see cref="IConfigureExecutionOptions"/> implementations to a <see cref="IConfigureExecution"/> implementation.
    /// </summary>
    [Obsolete("Remove in v6")]
    internal sealed class ConfigureExecutionOptionsMapper : IConfigureExecution
    {
        private readonly Func<Func<ExecutionOptions, Task<ExecutionResult>>, ExecutionOptions, Task<ExecutionResult>> _action;

        public ConfigureExecutionOptionsMapper(IEnumerable<IConfigureExecutionOptions> configureExecutionOptions)
        {
            var configurations = configureExecutionOptions.ToArray();
            if (configurations.Length > 0)
            {
                _action = async (next, options) =>
                {
                    for (int i = 0; i < configurations.Length; i++)
                    {
                        await configurations[i].ConfigureAsync(options).ConfigureAwait(false);
                    }
                    return await next(options).ConfigureAwait(false);
                };
            }
            else
            {
                _action = (next, options) => next(options);
            }
        }

        public Task<ExecutionResult> ExecuteAsync(Func<ExecutionOptions, Task<ExecutionResult>> next, ExecutionOptions executionOptions)
            => _action(next, executionOptions);
    }
}
