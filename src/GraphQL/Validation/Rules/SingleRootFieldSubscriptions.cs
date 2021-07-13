#nullable enable

using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public static readonly SingleRootFieldSubscriptions Instance = new SingleRootFieldSubscriptions();

        /// <inheritdoc/>
        /// <exception cref="SingleRootFieldSubscriptionsError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<Operation>((operation, context) =>
        {
            if (!IsSubscription(operation))
            {
                return;
            }

            int rootFields = operation.SelectionSet.Selections.Count;

            if (rootFields != 1)
            {
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation,
                    operation.SelectionSet.SelectionsList.Skip(1).ToArray()));
            }

            var fragment = operation.SelectionSet.SelectionsList.FirstOrDefault(IsFragment);

            if (fragment == null)
            {
                return;
            }

            if (fragment is FragmentSpread fragmentSpread)
            {
                var fragmentDefinition = context.GetFragment(fragmentSpread.Name);
                rootFields = fragmentDefinition?.SelectionSet.Selections.Count ?? 0;
            }
            else if (fragment is InlineFragment fragmentSelectionSet)
            {
                rootFields = fragmentSelectionSet.SelectionSet.Selections.Count;
            }

            if (rootFields != 1)
            {
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation, fragment));
            }

        }).ToTask();

        private static bool IsSubscription(Operation operation) => operation.OperationType == OperationType.Subscription;

        private static bool IsFragment(ISelection selection) => selection is FragmentSpread || selection is InlineFragment;
    }
}
