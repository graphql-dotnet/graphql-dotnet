#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;

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

        /// <summary>
        /// Returns the children fields for a specified node.
        /// </summary>
        Dictionary<string, Field>? GetSubFields(ExecutionContext executionContext, ExecutionNode executionNode);
    }
}
