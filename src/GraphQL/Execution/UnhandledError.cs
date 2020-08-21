using System;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an unhandled exception caught during document processing.
    /// </summary>
    [Serializable]
    public class UnhandledError : ExecutionError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledError"/> class with a specified error message. Sets the
        /// <see cref="ExecutionError.Code">Code</see> and <see cref="ExecutionError.Codes">Codes</see> properties based on
        /// the inner exception(s). Loads any exception data from the inner exception into this instance.
        /// </summary>
        public UnhandledError(string message, Exception innerException)
            : base(message, innerException)
        {
            // do not set another code by default; only inner exception's codes will be returned
        }
    }
}
