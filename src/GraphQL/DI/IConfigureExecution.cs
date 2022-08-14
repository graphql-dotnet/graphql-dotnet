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

        /// <summary>
        /// Determines the order of the registered <see cref="IConfigureExecution"/> instances;
        /// the lowest order executes first; instances with the same value execute in the same
        /// order they were registered, assuming the dependency injection provider returns
        /// instances in the order they were registered.
        /// <para>
        /// The default sort order of configurations are as follows:
        /// </para>
        /// <list type="bullet">
        /// <item>100: Option configurations -- 'Add' calls such as <see cref="GraphQLBuilderExtensions.AddValidationRule{TValidationRule}(IGraphQLBuilder, bool)">AddValidationRule</see>, and <see cref="GraphQLBuilderExtensions.ConfigureExecutionOptions(IGraphQLBuilder, Action{ExecutionOptions})">ConfigureExecutionOptions</see> calls</item>
        /// <item>200: Execution configurations -- 'Use' calls such as <see cref="GraphQLBuilderExtensions.UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>, and <see cref="GraphQLBuilderExtensions.ConfigureExecution(IGraphQLBuilder, Func{ExecutionOptions, ExecutionDelegate, Task{ExecutionResult}})">ConfigureExecution</see> calls</item>
        /// </list>
        /// </summary>
        float SortOrder { get; }
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

        public float SortOrder => GraphQLBuilderExtensions.SORT_ORDER_CONFIGURATION;
    }
}
