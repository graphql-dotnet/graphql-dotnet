using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Processes a parsed GraphQL request, resolving all the nodes and returning the result; exceptions
    /// are unhandled. Should not run any <see cref="IDocumentExecutionListener">IDocumentExecutionListener</see>s.
    /// </summary>
    public interface IExecutionStrategy
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result
        /// </summary>
        /// <param name="context">The execution parameters</param>
        Task<ExecutionResult> ExecuteAsync(ExecutionContext context);

        /// <summary>
        /// Executes an execution node and all of its child nodes. This is typically only executed upon
        /// the root execution node.
        /// </summary>
        Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode);

        /// <summary>
        /// Returns the children fields for a specified node.
        /// </summary>
        Dictionary<string, (GraphQLField field, FieldType fieldType)>? GetSubFields(ExecutionContext executionContext, ExecutionNode executionNode);
    }
}
