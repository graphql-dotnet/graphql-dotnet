using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class UnhandledError : ExecutionError
    {
        public UnhandledError(string message) : base(message) { }
        public UnhandledError(string message, Exception innerException) : base(message, innerException) { }
    }
}
