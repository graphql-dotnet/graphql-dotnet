using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class UniqueVariableNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.8.1";

        public UniqueVariableNamesError(ValidationContext context, VariableDefinition node, VariableDefinition altNode)
            : base(context.OriginalQuery, PARAGRAPH, DuplicateVariableMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateVariableMessage(string variableName)
            => $"There can be only one variable named \"{variableName}\"";
    }
}
