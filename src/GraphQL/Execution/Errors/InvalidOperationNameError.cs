namespace GraphQL.Execution;

/// <summary>
/// Represents an error triggered when a request specifies an operation name that is not listed in the document.
/// </summary>
[Serializable]
public class InvalidOperationNameError : DocumentError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidOperationNameError"/> class with a specified error message.
    /// </summary>
    public InvalidOperationNameError(string operationName)
        : base($"Document does not contain an operation named '{operationName}'.")
    {
        Code = "INVALID_OPERATION_NAME";
    }
}
