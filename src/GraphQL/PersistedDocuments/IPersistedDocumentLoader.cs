namespace GraphQL.PersistedDocuments;

/// <summary>
/// Defines a mechanism for loading persisted GraphQL documents.
/// </summary>
public interface IPersistedDocumentLoader
{
    /// <summary>
    /// Asynchronously retrieves a persisted query string based on the provided document identifier prefix and payload.
    /// </summary>
    /// <param name="documentIdPrefix">The prefix of the document identifier, which may be null for custom document identifiers.</param>
    /// <param name="documentIdPayload">The payload of the document identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the query string
    /// if found; otherwise, <see langword="null"/>.
    /// </returns>
    public ValueTask<string?> GetQueryAsync(string? documentIdPrefix, string documentIdPayload, CancellationToken cancellationToken);
}
