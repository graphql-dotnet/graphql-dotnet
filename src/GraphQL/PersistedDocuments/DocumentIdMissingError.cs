using GraphQL.Execution;

namespace GraphQL.PersistedDocuments;

/// <summary>
/// Represents an error that occurs when a request does not contain a documentId parameter.
/// </summary>
public class DocumentIdMissingError : RequestError
{
    /// <inheritdoc cref="DocumentIdMissingError"/>
    public DocumentIdMissingError() : base("The request must have a documentId parameter.")
    {
    }
}
