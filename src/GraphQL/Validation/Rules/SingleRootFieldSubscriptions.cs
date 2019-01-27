namespace GraphQL.Validation.Rules
{
    using GraphQL.Language.AST;
    using System.Linq;

    /// <summary>
    /// Subscription operations must have exactly one root field.
    /// </summary>
    public class SingleRootFieldSubscriptions : IValidationRule
    {
        private static readonly string RuleCode = "5.2.3.1";

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(config =>
            {
                config.Match<Operation>(operation =>
                {
                    if (!IsSubscription(operation))
                    {
                        return;
                    }

                    int rootFields = operation.SelectionSet.Selections.Count();

                    if (rootFields != 1)
                    {
                        context.ReportError(
                            new ValidationError(
                                context.OriginalQuery,
                                RuleCode,
                                InvalidNumberOfRootFieldMessaage(operation.Name),
                                operation.SelectionSet.Selections.Skip(1).ToArray()));
                    }
                });
            });
        }

        public static string InvalidNumberOfRootFieldMessaage(string name)
        {
            string prefix = name != null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }

        private static bool IsSubscription(Operation operation) =>
            operation.OperationType == OperationType.Subscription;
    }
}
