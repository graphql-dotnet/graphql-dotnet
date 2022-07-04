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
            : base(context.Document.Source, NUMBER, UnusedVariableMessage(node.Variable.Name.StringValue, op.Name?.StringValue), node)
        {
        }

        internal static string UnusedVariableMessage(string varName, string? opName)
        {
            return string.IsNullOrEmpty(opName)
                ? $"Variable '${varName}' is never used."
                : $"Variable '${varName}' is never used in operation '${opName}'.";
        }
    }
}
