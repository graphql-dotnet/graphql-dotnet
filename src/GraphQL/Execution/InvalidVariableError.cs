using System;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error triggered by an invalid variable passed with the associated document.
    /// </summary>
    [Serializable]
    public class InvalidVariableError : DocumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable and error message.
        /// </summary>
        public InvalidVariableError(string variableName, string message) :
            base($"Variable '${variableName}' is invalid. {message}")
        {
            Code = "INVALID_VALUE";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVariableError"/> class for a specified variable and error message. Sets the
        /// <see cref="Codes"/> property based on the inner exception(s). Loads any exception data from the inner exception into this instance.
        /// </summary>
        public InvalidVariableError(string variableName, string message, Exception innerException) :
            base($"Variable '${variableName}' is invalid. {message}", innerException)
        {
            Code = "INVALID_VALUE";
        }
    }
}
