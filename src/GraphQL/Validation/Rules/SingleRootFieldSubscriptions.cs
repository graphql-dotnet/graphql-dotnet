namespace GraphQL.Validation.Rules
{
    using GraphQL.Language.AST;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription operations must have exactly one root field.
    /// </summary>
    public class SingleRootFieldSubscriptions : IValidationRule
    {
        private const string RuleCode = "5.2.3.1";

        public static readonly SingleRootFieldSubscriptions Instance = new SingleRootFieldSubscriptions();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return new EnterLeaveListener(config =>
            {
                config.Match<Operation>(operation =>
                {
                    if (!IsSubscription(operation))
                    {
                        return;
                    }

                    int rootFields = operation.SelectionSet.Selections.Count;

                    if (rootFields != 1)
                    {
                        context.ReportError(
                            new ValidationError(
                                context.OriginalQuery,
                                RuleCode,
                                InvalidNumberOfRootFieldMessage(operation.Name),
                                operation.SelectionSet.Selections.Skip(1).ToArray()));
                    }

                    var fragment = operation.SelectionSet.Selections.FirstOrDefault(IsFragment);

                    if (fragment == null)
                    {
                        return;
                    }

                    if (fragment is FragmentSpread fragmentSpread)
                    {
                        var fragmentDefinition = context.GetFragment(fragmentSpread.Name);
                        rootFields = fragmentDefinition.SelectionSet.Selections.Count;
                    }
                    else if (fragment is InlineFragment fragmentSelectionSet)
                    {
                        rootFields = fragmentSelectionSet.SelectionSet.Selections.Count;
                    }

                    if (rootFields != 1)
                    {
                        context.ReportError(
                            new ValidationError(
                                context.OriginalQuery,
                                RuleCode,
                                InvalidNumberOfRootFieldMessage(operation.Name),
                                fragment));
                    }

                });
            }).ToTask();
        }

        public static string InvalidNumberOfRootFieldMessage(string name)
        {
            string prefix = name != null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }

        private static bool IsSubscription(Operation operation) =>
            operation.OperationType == OperationType.Subscription;

        private static bool IsFragment(ISelection selection) =>
            (selection is FragmentSpread) || (selection is InlineFragment);
    }
}
