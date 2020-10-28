using System.Threading.Tasks;

namespace GraphQL.Execution
{
    /// <summary>
    /// Processes a parsed GraphQL request, resolving all the nodes and returning the result; exceptions
    /// are unhandled. Should not run any <see cref="IDocumentExecutionListener">IDocumentExecutionListener</see>s except
    /// for <see cref="IDocumentExecutionListener.BeforeExecutionStepAwaitedAsync(IExecutionContext)">BeforeExecutionStepAwaitedAsync</see>.
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
