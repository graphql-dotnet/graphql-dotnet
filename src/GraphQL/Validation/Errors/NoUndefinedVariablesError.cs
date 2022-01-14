using System;
using GraphQL.Language.AST;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.NoUndefinedVariables"/>
    [Serializable]
    public class NoUndefinedVariablesError : ValidationError
    {
        internal const string NUMBER = "5.8.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public NoUndefinedVariablesError(ValidationContext context, GraphQLOperationDefinition node, VariableReference variableReference)
            : base(context.OriginalQuery!, NUMBER, UndefinedVarMessage(variableReference.Name, node.Name), variableReference, node)
        {
        }

        internal static string UndefinedVarMessage(ROM varName, ROM? opName)
        {
            return opName == null || opName.Value.Length == 0
                ? $"Variable '${varName}' is not defined."
                : $"Variable '${varName}' is not defined by operation '{opName}'.";
        }
    }
}
