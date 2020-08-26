using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueOperationNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.2.1.1";

        public UniqueOperationNamesError(ValidationContext context, Operation node)
            : base(context.OriginalQuery, PARAGRAPH, DuplicateOperationNameMessage(node.Name), node)
        {
        }

        internal static string DuplicateOperationNameMessage(string opName)
            => $"There can only be one operation named {opName}.";
    }
}
