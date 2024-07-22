namespace GraphQL.PersistedDocuments;

/// <summary>
/// A set of options for <see cref="PersistedDocumentHandler"/>.
/// </summary>
public class PersistedDocumentOptions
{
    private const string ERROR_NO_REQUESTSERVICES = $"{nameof(ExecutionOptions)}.{nameof(ExecutionOptions.RequestServices)} or {nameof(PersistedDocumentOptions)}.{nameof(GetQueryDelegate)} must be set.";
    private const string ERROR_NO_LOADER = $"{nameof(IPersistedDocumentLoader)} must be registered with your DI provider or {nameof(PersistedDocumentOptions)}.{nameof(GetQueryDelegate)} must be set.";

    /// <summary>
    /// Indicates if requests that are not persisted document requests should be allowed to be executed.
    /// </summary>
    public bool AllowNonpersistedDocuments { get; set; }

    /// <summary>
    /// Returns a list of document id prefixes that are allowed to be used.
    /// Defaults to a single value, "sha256".
    /// Recognized values (only "sha256" currently) are validated pursuant to the specification.
    /// Add <see langword="null"/> to support custom document identifiers.
    /// Custom prefixes should start with "x-" pursuant to the specification.
    /// Clear this list to allow any type of document identifier.
    /// </summary>
    public HashSet<string?> AllowedPrefixes { get; } = new(StringComparer.Ordinal) { "sha256" };

    /// <summary>
    /// Gets or sets a delegate used to return a query string from a document identifier.
    /// The delegate is passed the <see cref="ExecutionOptions"/> instance along with
    /// the document identifier parsed into a prefix and payload. The original document
    /// identifier can be pulled from <see cref="ExecutionOptions.DocumentId"/> if required.
    /// <para>
    /// By default the delegate attempts to pull <see cref="IPersistedDocumentLoader"/> from
    /// the <see cref="ExecutionOptions.RequestServices"/> service provider and uses that to
    /// load the requested document.
    /// </para>
    /// </summary>
    public Func<ExecutionOptions, string?, string, ValueTask<string?>> GetQueryDelegate { get; set; } = (options, prefix, payload) =>
    {
        var provider = options.RequestServices
            ?? throw new InvalidOperationException(ERROR_NO_REQUESTSERVICES);
        var retriever = (IPersistedDocumentLoader?)provider.GetService(typeof(IPersistedDocumentLoader))
            ?? throw new InvalidOperationException(ERROR_NO_LOADER);
        return retriever.GetQueryAsync(prefix, payload, options.CancellationToken);
    };
}
