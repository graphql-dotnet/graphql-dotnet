using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public interface IExecutionStrategy
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="context">The execution parameters</param>
        Task<ExecutionResult> ExecuteAsync(ExecutionContext context);
    }
}
