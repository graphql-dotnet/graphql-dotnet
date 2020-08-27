using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class VariablesAreInputTypesError : ValidationError
    {
        internal const string NUMBER = "5.8.2";

        public VariablesAreInputTypesError(ValidationContext context, VariableDefinition node, IGraphType type)
            : base(context.OriginalQuery, NUMBER, UndefinedVarMessage(node.Name, type != null ? context.Print(type) : node.Type.Name()), node)
        {
        }

        internal static string UndefinedVarMessage(string variableName, string typeName)
            => $"Variable \"{variableName}\" cannot be non-input type \"{typeName}\".";
    }
}
