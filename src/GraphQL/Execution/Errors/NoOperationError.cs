namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error triggered when the document does not include any operations.
    /// </summary>
    [Serializable]
    public class NoOperationError : DocumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoOperationError"/> class.
        /// </summary>
        public NoOperationError()
            : base("Document does not contain any operations.")
        {
            Code = "NO_OPERATION";
        }
    }
}
