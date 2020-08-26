using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class SingleRootFieldSubscriptionsError : ValidationError
    {
        public const string PARAGRAPH = "5.2.3.1";

        public SingleRootFieldSubscriptionsError(ValidationContext context, Operation operation, params ISelection[] nodes)
            : base(context.OriginalQuery, PARAGRAPH, InvalidNumberOfRootFieldMessage(operation.Name), nodes)
        {
        }

        internal static string InvalidNumberOfRootFieldMessage(string name)
        {
            string prefix = name != null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }
    }
}
