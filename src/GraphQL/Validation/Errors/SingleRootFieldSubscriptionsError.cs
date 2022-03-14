using GraphQLParser.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.SingleRootFieldSubscriptions"/>
    [Serializable]
    public class SingleRootFieldSubscriptionsError : ValidationError
    {
        internal const string NUMBER = "5.2.3.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public SingleRootFieldSubscriptionsError(ValidationContext context, GraphQLOperationDefinition operation, params ASTNode[] nodes)
            : base(context.Document.Source, NUMBER, InvalidNumberOfRootFieldMessage(operation.Name), nodes)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public SingleRootFieldSubscriptionsError(ExecutionContext context)
            : base(context.Document.Source, NUMBER, InvalidNumberOfRootFieldMessage(context.Operation.Name), context.Operation.SelectionSet.Selections.Skip(1).ToArray())
        {
        }

        internal static string InvalidNumberOfRootFieldMessage(GraphQLName? name)
        {
            string prefix = name is not null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }
    }
}
