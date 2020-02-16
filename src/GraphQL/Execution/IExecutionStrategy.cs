using System.Threading.Tasks;

namespace GraphQL.Execution
{
    /// <summary>
    /// Processes a given parsed GraphQL request, resolve all the nodes and return the result; exceptions are unhandled
    /// </summary>
    public interface IExecutionStrategy
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="context">The execution parameters</param>
        Task<ExecutionResult> ExecuteAsync(ExecutionContext context);
    }
}
