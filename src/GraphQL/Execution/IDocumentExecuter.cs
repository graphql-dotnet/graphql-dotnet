using System;
using System.Threading.Tasks;

namespace GraphQL
{
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
