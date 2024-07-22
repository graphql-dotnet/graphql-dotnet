using GraphQL.Execution;

namespace GraphQL.PersistedDocuments;

/// <summary>
/// Represents an error that occurs when a request contains a documentId parameter that does not correspond to a persisted document.
/// </summary>
public class DocumentNotFoundError : RequestError
{
    /// <inheritdoc cref="DocumentNotFoundError"/>
    public DocumentNotFoundError() : base("The documentId parameter does not correspond to a persisted document.")
    {
    }
}
