namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error triggered by an invalid operation being requested that is not configured for the schema.
    /// </summary>
    [Serializable]
    public class InvalidOperationError : DocumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidOperationError"/> class with a specified error message.
        /// </summary>
        public InvalidOperationError(string message)
            : base(message)
        {
            Code = "INVALID_OPERATION";
        }
    }
}
