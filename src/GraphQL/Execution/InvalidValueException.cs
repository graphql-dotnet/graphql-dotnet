using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class InvalidValueException : ExecutionError
    {
        public InvalidValueException(string variableName, string message) :
            base($"Variable '${variableName}' is invalid. {message}")
        {

        }
    }
}
