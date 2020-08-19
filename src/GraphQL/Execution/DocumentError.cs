using System;

namespace GraphQL.Execution
{
    public abstract class DocumentError : ExecutionError
    {
        public DocumentError(string message) : base(message) { }
        
        public DocumentError(string message, Exception innerException) : base(message, innerException) { }
    }
}
