using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class UniqueOperationNamesError : ValidationError
    {
        internal const string NUMBER = "5.2.1.1";

        public UniqueOperationNamesError(ValidationContext context, Operation node)
            : base(context.OriginalQuery, NUMBER, DuplicateOperationNameMessage(node.Name), node)
        {
        }

        internal static string DuplicateOperationNameMessage(string opName)
            => $"There can only be one operation named {opName}.";
    }
}
