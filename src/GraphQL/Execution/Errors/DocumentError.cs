namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error generated while parsing or validating the document or its associated variables.
    /// </summary>
    public abstract class DocumentError : ExecutionError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentError"/> class with a specified error message.
        /// </summary>
        public DocumentError(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentError"/> class with a specified
        /// error message and inner exception. Sets the <see cref="ExecutionError.Code">Code</see>
        /// property based on the inner exception. Loads any exception data from the inner exception
        /// into this instance.
        /// </summary>
        public DocumentError(string message, Exception? innerException) : base(message, innerException) { }
    }
}
