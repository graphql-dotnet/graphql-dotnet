using System;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.NoUnusedVariables"/>
    [Serializable]
    public class NoUnusedVariablesError : ValidationError
    {
        internal const string NUMBER = "5.8.4";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public NoUnusedVariablesError(ValidationContext context, GraphQLVariableDefinition node, GraphQLOperationDefinition op)
            : base(context.OriginalQuery!, NUMBER, UnusedVariableMessage(node.Variable.Name, op.Name), node)
        {
        }

        internal static string UnusedVariableMessage(ROM varName, GraphQLName? opName)
        {
            return opName is null
                ? $"Variable '${varName}' is never used."
                : $"Variable '${varName}' is never used in operation '${opName}'.";
        }
    }
}
