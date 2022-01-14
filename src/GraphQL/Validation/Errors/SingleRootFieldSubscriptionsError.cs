using System;
using GraphQLParser;
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
            : base(context.OriginalQuery!, NUMBER, InvalidNumberOfRootFieldMessage(operation.Name), nodes)
        {
        }

        internal static string InvalidNumberOfRootFieldMessage(ROM name)
        {
            string prefix = name.IsEmpty ? "Anonymous Subscription" : $"Subscription '{name}'";
            return $"{prefix} must select only one top level field.";
        }
    }
}
