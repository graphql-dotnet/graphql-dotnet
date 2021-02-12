using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents an error triggered by an invalid variable passed with the associated document.
    /// </summary>
    [Serializable]
    public class InvalidVariableError : ValidationError
    {
        private const string NUMBER = "5.6.1";

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable and error message.
        /// </summary>
        public InvalidVariableError(ValidationContext context, VariableDefinition node, VariableName variableName, string message) :
            base(context.OriginalQuery, NUMBER, $"Variable '${variableName}' is invalid. {message}", node)
        {
            Code = "INVALID_VALUE";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable
        /// and error message. Loads any exception data from the inner exception into this instance.
        /// </summary>
        public InvalidVariableError(ValidationContext context, VariableDefinition node, VariableName variableName, string message, Exception innerException) :
            base(context.OriginalQuery, NUMBER, $"Variable '${variableName}' is invalid. {message}", innerException, node)
        {
            Code = "INVALID_VALUE";
        }
    }
}
