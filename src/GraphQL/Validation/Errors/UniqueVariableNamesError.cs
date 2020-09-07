using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class UniqueVariableNamesError : ValidationError
    {
        internal const string NUMBER = "5.8.1";

        public UniqueVariableNamesError(ValidationContext context, VariableDefinition node, VariableDefinition altNode)
            : base(context.OriginalQuery, NUMBER, DuplicateVariableMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateVariableMessage(string variableName)
            => $"There can be only one variable named \"{variableName}\"";
    }
}
