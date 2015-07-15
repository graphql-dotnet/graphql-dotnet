using System;

namespace GraphQL
{
    public class ExecutionError : Exception
    {
        public ExecutionError(string message)
            : base(message)
        {
        }

        public ExecutionError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
