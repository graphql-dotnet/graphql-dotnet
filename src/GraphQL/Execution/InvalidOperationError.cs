using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class InvalidOperationError : DocumentError
    {
        public InvalidOperationError(string message)
            : base(message)
        {
            Code = "INVALID_OPERATION";
        }
    }
}
