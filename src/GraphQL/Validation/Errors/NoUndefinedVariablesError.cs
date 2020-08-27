using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class NoUndefinedVariablesError : ValidationError
    {
        internal const string NUMBER = "5.8.3";

        public NoUndefinedVariablesError(ValidationContext context, Operation node, VariableReference variableReference)
            : base(context.OriginalQuery, NUMBER, UndefinedVarMessage(variableReference.Name, node.Name), variableReference, node)
        {
        }

        internal static string UndefinedVarMessage(string varName, string opName)
            => !string.IsNullOrWhiteSpace(opName)
                ? $"Variable \"${varName}\" is not defined by operation \"{opName}\"."
                : $"Variable \"${varName}\" is not defined.";
    }
}
