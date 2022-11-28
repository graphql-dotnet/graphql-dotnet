using GraphQLParser.AST;

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

        internal static string InvalidNumberOfRootFieldMessage(GraphQLName? name)
        {
            string prefix = name is not null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }
    }
}
