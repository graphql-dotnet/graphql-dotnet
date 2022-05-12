using System.Security.Cryptography;
using System.Text;
using GraphQL.DI;

namespace GraphQL.Caching;

/// <summary>
/// Implementation of Automatic Persisted Queries (APQ).
/// https://www.apollographql.com/docs/react/api/link/persisted-queries/
/// </summary>
public class AutomaticPersistedQueriesExecution : IConfigureExecution
{
    private readonly IQueryCache _cache;
    private const string SUPPORTED_VERSION = "1";

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public AutomaticPersistedQueriesExecution(IQueryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Searching APQ fields in <see cref="ExecutionOptions.Extensions"/> based on a protocol:
    /// https://www.apollographql.com/docs/react/api/link/persisted-queries/#protocol
    /// </summary>
    protected virtual (string? Hash, string? Version, bool Enabled) GetAPQProperties(Inputs? extensions)
    {
        string? hashResult = null;
        string? versionResult = null;
        bool enabled = false;

        if (extensions?.TryGetValue("persistedQuery", out var persistedQueryObject) ?? false)
        {
            enabled = true;

            if (persistedQueryObject is Dictionary<string, object> persistedQuery)
            {
                if (persistedQuery.TryGetValue("sha256Hash", out var sha256HashObject) && sha256HashObject is string sha256Hash && !string.IsNullOrWhiteSpace(sha256Hash))
                {
                    hashResult = sha256Hash;
                }

                if (persistedQuery.TryGetValue("version", out var versionObject) && versionObject is string version && !string.IsNullOrWhiteSpace(version))
                {
                    versionResult = version;
                }
            }
        }

        return (hashResult, versionResult, enabled);
    }

    /// <summary>
    /// Compute SHA256 hash.
    /// </summary>
    protected virtual string ComputeSHA256(string input)
    {
        var bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

        var builder = new StringBuilder();
        foreach (var item in bytes)
        {
            builder.Append(item.ToString("x2"));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Create <see cref="ExecutionResult"/> with provided error.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new ExecutionResult { Errors = new ExecutionErrors { error } };

    /// <inheritdoc/>
    public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        var apq = GetAPQProperties(options.Extensions);

        if (apq.Enabled && apq.Version != SUPPORTED_VERSION)
        {
            return CreateExecutionResult(new PersistedQueryUnsupportedVersionError(apq.Version));
        }

        if (string.IsNullOrWhiteSpace(options.Query))
        {
            if (string.IsNullOrWhiteSpace(apq.Hash))
            {
                return CreateExecutionResult(new ExecutionError("GraphQL query is missing."));
            }
            else
            {
                var queryFromCache = await _cache.GetAsync(apq.Hash!).ConfigureAwait(false);

                if (queryFromCache == null)
                {
                    return CreateExecutionResult(new PersistedQueryNotFoundError(apq.Hash!));
                }
                else
                {
                    options.Query = queryFromCache;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(apq.Hash))
        {
            if (apq.Hash!.Equals(ComputeSHA256(options.Query!), StringComparison.InvariantCultureIgnoreCase))
            {
                await _cache.SetAsync(apq.Hash!, options.Query!).ConfigureAwait(false);
            }
            else
            {
                return CreateExecutionResult(new PersistedQueryBadHashError(apq.Hash));
            }
        }

        return await next(options).ConfigureAwait(false);
    }
}
