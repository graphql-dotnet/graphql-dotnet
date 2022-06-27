using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Processes an entire GraphQL request, given an input GraphQL request string. This is intended to
    /// be called by user code to process a query.
    /// <br/><br/>
    /// Typical implementation starts metrics if enabled (see <see cref="Instrumentation.Metrics">Metrics</see>),
    /// relies on <see cref="Execution.IDocumentBuilder">IDocumentBuilder</see> to parse the query and
    /// <see cref="Validation.IDocumentValidator">IDocumentValidator</see> to validate it.
    /// Then it executes document listeners if attached, selects an execution strategy, and executes the query
    /// via <see cref="Execution.IExecutionStrategy">IExecutionStrategy</see>. Unhandled exceptions are handled as appropriate for the selected options.
    /// </summary>
    public interface IDocumentExecuter
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="options">The options of the execution</param>
        Task<ExecutionResult> ExecuteAsync(ExecutionOptions options);
    }

    /// <summary>
    /// Process an entire GraphQL request against a specific schema, given an input GraphQL request string.
    /// This is intended to be called by user code to process a query.
    /// </summary>
    public interface IDocumentExecuter<TSchema> : IDocumentExecuter
        where TSchema : ISchema
    {
    }

    /// <summary>
    /// Extension methods for <see cref="IDocumentExecuter"/>.
    /// </summary>
    public static class DocumentExecuterExtensions
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="executer">An instance of <see cref="IDocumentExecuter"/> to use to execute the query</param>
        /// <param name="configure">A delegate which configures the execution options</param>
        public static Task<ExecutionResult> ExecuteAsync(this IDocumentExecuter executer, Action<ExecutionOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new ExecutionOptions();
            configure(options);
            return executer.ExecuteAsync(options);
        }
    }
}
