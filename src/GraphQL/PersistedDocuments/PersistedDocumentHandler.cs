using GraphQL.DI;

namespace GraphQL.PersistedDocuments;

/// <summary>
/// Handles persisted document requests.
/// </summary>
public class PersistedDocumentHandler : IConfigureExecution
{
    private readonly PersistedDocumentOptions _options;

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    public PersistedDocumentHandler(PersistedDocumentOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance with the default options.
    /// Useful when only sha256 document identifiers are in use and
    /// <see cref="IPersistedDocumentLoader"/> has been registered to
    /// load documents.
    /// </summary>
    public PersistedDocumentHandler()
        : this(new PersistedDocumentOptions())
    {
    }

    /// <summary>
    /// Create <see cref="ExecutionResult"/> with provided error.
    /// Override this method to change the provided error responses.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new(error);

    private static readonly Dictionary<string, Func<string, bool>> _documentIdValidators = new()
    {
        {
            "sha256",
            payload =>
            {
                // validate sha256 hash length
                if (payload.Length != 64)
                    return false;

                // validate only lowercase hexadecimal characters are present
                for (int i = 0; i < payload.Length; i++)
                {
                    var c = payload[i];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                        return false;
                }

                return true;
            }
        }
    };

    /// <inheritdoc/>
    public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        // ensure that both documentId and query are not provided
        if (options.DocumentId != null && options.Query != null)
            return CreateExecutionResult(new InvalidRequestError());

        // if query is provided, but documentId is not, then we need to execute or refuse the request
        if (options.Query != null)
        {
            if (!_options.AllowNonpersistedDocuments)
                return CreateExecutionResult(new DocumentIdMissingError());
            else
                return await next(options).ConfigureAwait(false);
        }

        // if the document id does not exist, return an appropriate error
        if (options.DocumentId == null)
            return CreateExecutionResult(new DocumentIdMissingError());

        // parse the document id into a prefix and payload
        var (prefix, payload) = ParseDocumentId(options.DocumentId);

        // ensure the prefix and payload is valid
        if (prefix?.Length == 0 || payload.Length == 0 || payload.IndexOf(':') != -1)
            return CreateExecutionResult(new DocumentIdInvalidError());

        // ensure the prefix is whitelisted, if applicable
        if (_options.AllowedPrefixes.Count > 0 && !_options.AllowedPrefixes.Contains(prefix))
            return CreateExecutionResult(new DocumentIdInvalidError());

        // validate the payload format of known prefixes (currently only sha256)
        if (prefix != null && _documentIdValidators.TryGetValue(prefix, out var validationFunc) && !validationFunc(payload))
            return CreateExecutionResult(new DocumentIdInvalidError());

        // retrieve the query from the underlying storage provider
        var query = await _options.GetQueryDelegate(options, prefix, payload).ConfigureAwait(false);

        // if the document id was not found, return an appropriate error
        if (query == null)
            return CreateExecutionResult(new DocumentNotFoundError());

        // set the query string and continue execution
        options.Query = query;
        return await next(options).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the document identifier into a prefix and payload.
    /// </summary>
    private (string? prefix, string payload) ParseDocumentId(string documentId)
    {
        var index = documentId.IndexOf(':');
        return index == -1
            ? (null, documentId)
            : (documentId.Substring(0, index), documentId.Substring(index + 1));
    }

    /// <inheritdoc/>
    public virtual float SortOrder => 200;
}
