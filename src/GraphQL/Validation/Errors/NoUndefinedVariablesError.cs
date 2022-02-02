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
        public NoUndefinedVariablesError(ValidationContext context, GraphQLOperationDefinition node, GraphQLVariable variableReference)
            : base(context.Document.Source, NUMBER, UndefinedVarMessage(variableReference.Name.StringValue, node.Name?.StringValue), variableReference, node)
        {
        }

        internal static string UndefinedVarMessage(string varName, string? opName)
        {
            return string.IsNullOrEmpty(opName)
                ? $"Variable '${varName}' is not defined."
                : $"Variable '${varName}' is not defined by operation '{opName}'.";
        }
    }
}
