using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class UnhandledError : ExecutionError
    {
        public UnhandledError(string message, Exception innerException)
            : base(message, innerException)
        {
            // do not set another code by default; only inner exception's codes will be returned
        }
    }
}
