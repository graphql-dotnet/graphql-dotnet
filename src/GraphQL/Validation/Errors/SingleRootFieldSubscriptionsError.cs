using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class SingleRootFieldSubscriptionsError : ValidationError
    {
        internal const string NUMBER = "5.2.3.1";

        public SingleRootFieldSubscriptionsError(ValidationContext context, Operation operation, params ISelection[] nodes)
            : base(context.OriginalQuery, NUMBER, InvalidNumberOfRootFieldMessage(operation.Name), nodes)
        {
        }

        internal static string InvalidNumberOfRootFieldMessage(string name)
        {
            string prefix = name != null ? $"Subscription '{name}'" : "Anonymous Subscription";
            return $"{prefix} must select only one top level field.";
        }
    }
}
