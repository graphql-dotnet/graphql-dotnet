using GraphQL.Execution;

namespace GraphQL.PersistedDocuments;

/// <summary>
/// Represents an error that occurs when a request has a documentId parameter which is formatted incorrectly.
/// </summary>
public class DocumentIdInvalidError : RequestError
{
    /// <inheritdoc cref="DocumentIdInvalidError"/>
    public DocumentIdInvalidError() : base("The format of the documentId parameter is invalid.")
    {
    }
}
