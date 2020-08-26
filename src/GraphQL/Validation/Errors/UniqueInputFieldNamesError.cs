using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueInputFieldNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.6.3";

        public UniqueInputFieldNamesError(ValidationContext context, IValue node, ObjectField altNode)
            : base(context.OriginalQuery, PARAGRAPH, DuplicateInputField(altNode.Name), node, altNode.Value)
        {
        }

        internal static string DuplicateInputField(string fieldName)
            => $"There can be only one input field named {fieldName}.";
    }
}
