using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Subscription operations must have exactly one root field.
    /// </summary>
    public class SingleRootFieldSubscriptions : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly SingleRootFieldSubscriptions Instance = new();

        /// <inheritdoc/>
        /// <exception cref="SingleRootFieldSubscriptionsError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((operation, context) =>
        {
            if (!IsSubscription(operation))
            {
                return;
            }

            int rootFields = operation.SelectionSet.Selections.Count;

            if (rootFields != 1)
            {
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation,
                    operation.SelectionSet.Selections.Skip(1).ToArray()));
            }

            var fragment = operation.SelectionSet.Selections.FirstOrDefault(IsFragment);

            if (fragment == null)
            {
                return;
            }

            if (fragment is GraphQLFragmentSpread fragmentSpread)
            {
                var fragmentDefinition = context.Document.FindFragmentDefinition(fragmentSpread.FragmentName.Name);
                rootFields = fragmentDefinition?.SelectionSet.Selections.Count ?? 0;
            }
            else if (fragment is GraphQLInlineFragment fragmentSelectionSet)
            {
                rootFields = fragmentSelectionSet.SelectionSet.Selections.Count;
            }

            if (rootFields != 1)
            {
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation, fragment));
            }

        });

        private static bool IsSubscription(GraphQLOperationDefinition operation) => operation.Operation == OperationType.Subscription;

        private static bool IsFragment(ASTNode selection) => selection is GraphQLFragmentSpread || selection is GraphQLInlineFragment;
    }
}
