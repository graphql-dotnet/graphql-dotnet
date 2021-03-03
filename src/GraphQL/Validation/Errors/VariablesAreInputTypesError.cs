using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.VariablesAreInputTypes"/>
    [Serializable]
    public class VariablesAreInputTypesError : ValidationError
    {
        internal const string NUMBER = "5.8.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public VariablesAreInputTypesError(ValidationContext context, VariableDefinition node, IGraphType type)
            : base(context.Document.OriginalQuery, NUMBER, UndefinedVarMessage(node.Name, type?.ToString() ?? node.Type.Name()), node)
        {
        }

        internal static string UndefinedVarMessage(string variableName, string typeName)
            => $"Variable \"{variableName}\" cannot be non-input type \"{typeName}\".";
    }
}
