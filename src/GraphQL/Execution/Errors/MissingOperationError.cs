namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error triggered when the document includes multiple operations, but no operation name was specified in the request.
    /// </summary>
    [Serializable]
    public class NoOperationNameError : DocumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoOperationNameError"/> class.
        /// </summary>
        public NoOperationNameError()
            : base("Document contains more than one operation, but the operation name was not specified.")
        {
            Code = "NO_OPERATION_NAME";
        }
    }
}
