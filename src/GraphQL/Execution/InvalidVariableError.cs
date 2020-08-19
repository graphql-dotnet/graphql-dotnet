using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class InvalidVariableError : DocumentError
    {
        public InvalidVariableError(string variableName, string message) :
            base($"Variable '${variableName}' is invalid. {message}")
        {
            Code = "INVALID_VALUE";
        }

        public InvalidVariableError(string variableName, string message, Exception innerException) :
            base($"Variable '${variableName}' is invalid. {message}", innerException)
        {
            Code = "INVALID_VALUE";
            // note: this ExecutionError will also return Codes of inner exceptions, plus the Data in the inner exception
        }
    }
}
