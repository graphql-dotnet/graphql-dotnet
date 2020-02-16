using System;
using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Processes an entire GraphQL request, given an input GraphQL request string. This is intended to
    /// be called by user code to process a query.<br/>
    /// <br/>
    /// Typical implementation starts metrics if enabled, relies on IDocumentBuilder to parse the query,
    /// executes document listeners if attached, selects an execution strategy, and executes the query
    /// via IExecutionStrategy. Unhandled exceptions are handled as appropriate for the selected options.
    /// </summary>
    public interface IDocumentExecuter
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="options">The options of the execution</param>
        Task<ExecutionResult> ExecuteAsync(ExecutionOptions options);

        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="configure">A delegate which configures the execution options</param>
        Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure);
    }
}
