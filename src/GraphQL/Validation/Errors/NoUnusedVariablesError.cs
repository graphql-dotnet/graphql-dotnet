using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class NoUnusedVariablesError : ValidationError
    {
        internal const string NUMBER = "5.8.4";

        public NoUnusedVariablesError(ValidationContext context, VariableDefinition node, Operation op)
            : base(context.OriginalQuery, NUMBER, UnusedVariableMessage(node.Name, op.Name), node)
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
