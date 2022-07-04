using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents an error triggered by an invalid variable passed with the associated document.
    /// </summary>
    [Serializable]
    public class InvalidVariableError : ValidationError
    {
        // The specification does not contain rules for validating the actual variables values, so the number of the entire section of the specification is used.
        private const string NUMBER = "5.8";

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable and error message.
        /// </summary>
        public InvalidVariableError(ValidationContext context, GraphQLVariableDefinition node, VariableName variableName, string message)
            : base(context.Document.Source, NUMBER, $"Variable '${variableName}' is invalid. {message}", node)
        {
            Code = "INVALID_VALUE";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable
        /// and error message. Loads any exception data from the inner exception into this instance.
        /// </summary>
        public InvalidVariableError(ValidationContext context, GraphQLVariableDefinition node, VariableName variableName, string message, Exception innerException)
            : base(context.Document.Source, NUMBER, $"Variable '${variableName}' is invalid. {message}", innerException, node)
        {
            Code = "INVALID_VALUE";
        }
    }
}
