using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Subscription operations must have exactly one root field.
    /// </summary>
    public class SingleRootFieldSubscriptions : ValidationRuleBase
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly SingleRootFieldSubscriptions Instance = new();

        /// <inheritdoc/>
        /// <exception cref="SingleRootFieldSubscriptionsError"/>
        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((operation, context) =>
        {
            if (operation.Operation != OperationType.Subscription)
                return;

            var rootFields = operation.SelectionSet.Selections;

            if (rootFields.Count != 1)
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation, rootFields.Skip(1).ToArray()));

            if (rootFields[0] is GraphQLField field && IsIntrospectionField(field))
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation, field));

            var fragment = operation.SelectionSet.Selections.Find(node => node is GraphQLFragmentSpread || node is GraphQLInlineFragment);

            if (fragment == null)
                return;

            if (fragment is GraphQLFragmentSpread fragmentSpread)
            {
                var fragmentDefinition = context.Document.FindFragmentDefinition(fragmentSpread.FragmentName.Name);
                if (fragmentDefinition == null)
                    return;

                rootFields = fragmentDefinition.SelectionSet.Selections;
            }
            else if (fragment is GraphQLInlineFragment inlineFragment)
            {
                rootFields = inlineFragment.SelectionSet.Selections;
            }

            if (rootFields.Count != 1 || (rootFields[0] is GraphQLField field2 && IsIntrospectionField(field2)))
                context.ReportError(new SingleRootFieldSubscriptionsError(context, operation, fragment));
        });

        private static bool IsIntrospectionField(GraphQLField field) => field.Name.Value.Length >= 2 && field.Name.Value.Span[0] == '_' && field.Name.Value.Span[1] == '_';
    }
}
