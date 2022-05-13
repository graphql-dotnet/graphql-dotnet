namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an unhandled exception caught during document or subscription processing.
    /// </summary>
    [Serializable]
    public class UnhandledError : ExecutionError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledError"/> class with a specified error message. Sets the
        /// <see cref="ExecutionError.Code">Code</see> property based on the inner exception.
        /// Loads any exception data from the inner exception into this instance.
        /// </summary>
        public UnhandledError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
