using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class NoUnusedVariablesError : ValidationError
    {
        public const string PARAGRAPH = "5.8.4";

        public NoUnusedVariablesError(ValidationContext context, VariableDefinition node, Operation op)
            : base(context.OriginalQuery, PARAGRAPH, UnusedVariableMessage(node.Name, op.Name), node)
        {
        }

        internal static string UnusedVariableMessage(string varName, string opName)
        {
            return !string.IsNullOrWhiteSpace(opName)
              ? $"Variable \"${varName}\" is never used in operation \"${opName}\"."
              : $"Variable \"${varName}\" is never used.";
        }
    }
}
