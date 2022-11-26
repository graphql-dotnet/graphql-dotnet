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
        /// Returns the children fields for a specified node. Note that this set will be completely defined only for
        /// fields of a concrete type (i.e. not interface or union field) or when <paramref name="executionNode"/>
        /// result was set. For interface field this method returns requested fields in terms of this interface. For
        /// union field this method returns empty set since we don't know the concrete union member if <see cref="ExecutionNode.Result"/>
        /// was not yet set.
        /// </summary>
        Dictionary<string, (GraphQLField field, FieldType fieldType)>? GetSubFields(ExecutionContext executionContext, ExecutionNode executionNode);
    }
}
