using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class InvalidValueException : ExecutionError
    {
        public InvalidValueException(string fieldName, string message) :
            base($"Variable '${fieldName}' is invalid. {message}")
        {

        }
    }
}
