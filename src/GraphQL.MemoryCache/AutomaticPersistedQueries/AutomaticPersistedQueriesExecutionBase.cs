using System.Security.Cryptography;
using System.Text;
using GraphQL.DI;

namespace GraphQL.Caching;

/// <summary>
/// Implementation of Automatic Persisted Queries (APQ).
/// https://www.apollographql.com/docs/react/api/link/persisted-queries/
/// </summary>
public abstract class AutomaticPersistedQueriesExecutionBase : IConfigureExecution
{
    /// <summary>
    /// Supported version.
    /// </summary>
    public const string SUPPORTED_VERSION = "1";

    private SHA256? _sha256;

    /// <summary>
    /// Searching APQ fields in <see cref="ExecutionOptions.Extensions"/> based on a protocol:
    /// https://www.apollographql.com/docs/react/api/link/persisted-queries/#protocol
    /// </summary>
    protected virtual (string? Hash, string? Version, bool Enabled) GetAPQProperties(Inputs? extensions)
    {
        string? hashResult = null;
        string? versionResult = null;
        bool enabledResult = false;

        if (extensions?.TryGetValue("persistedQuery", out var persistedQueryObject) ?? false)
        {
            enabledResult = true;

            if (persistedQueryObject is Dictionary<string, object> persistedQuery)
            {
                if (persistedQuery.TryGetValue("sha256Hash", out var sha256HashObject) && sha256HashObject is string sha256Hash && !string.IsNullOrEmpty(sha256Hash))
                {
                    hashResult = sha256Hash;
                }

                if (persistedQuery.TryGetValue("version", out var versionObject) && versionObject is string version && !string.IsNullOrEmpty(version))
                {
                    versionResult = version;
                }
            }
        }

        return (hashResult, versionResult, enabledResult);
    }

    /// <summary>
    /// Check equality of the provided hash and a hash computed from the query.
    /// </summary>
    protected virtual bool CheckHash(string hash, string query)
    {
        var inputBytes = Encoding.UTF8.GetBytes(query);
        var shaShared = Interlocked.Exchange(ref _sha256, null) ?? SHA256.Create();
        var bytes = shaShared.ComputeHash(inputBytes);
        Interlocked.CompareExchange(ref _sha256, shaShared, null);

#if NET5_0_OR_GREATER
        var queryHash = Convert.ToHexString(bytes);
#else
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var item in bytes)
        {
            builder.Append(item.ToString("x2"));
        }
        var queryHash = builder.ToString();
#endif

        return hash.Equals(queryHash, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Create <see cref="ExecutionResult"/> with provided error.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new ExecutionResult { Errors = new ExecutionErrors { error } };

    /// <summary>
    /// Get query by hash. It is likely to be implemented via cache mechanism.
    /// </summary>
    protected abstract ValueTask<string?> GetQueryAsync(string hash);

    /// <summary>
    /// Set query by hash. It is likely to be implemented via cache mechanism.
    /// </summary>
    protected abstract Task SetQueryAsync(string hash, string query);

    /// <inheritdoc/>
    public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        var apq = GetAPQProperties(options.Extensions);

        if (!apq.Enabled)
        {
            return await next(options).ConfigureAwait(false);
        }

        if (apq.Version != SUPPORTED_VERSION)
        {
            return CreateExecutionResult(new PersistedQueryUnsupportedVersionError(apq.Version));
        }

        if (options.Query == null)
        {
            if (apq.Hash != null)
            {
                var queryFromCache = await GetQueryAsync(apq.Hash).ConfigureAwait(false);

                if (queryFromCache == null)
                {
                    return CreateExecutionResult(new PersistedQueryNotFoundError(apq.Hash));
                }
                else
                {
                    options.Query = queryFromCache;
                }
            }
        }
        else if (apq.Hash != null)
        {
            if (CheckHash(apq.Hash, options.Query))
            {
                await SetQueryAsync(apq.Hash, options.Query).ConfigureAwait(false);
            }
            else
            {
                return CreateExecutionResult(new PersistedQueryBadHashError(apq.Hash));
            }
        }

        return await next(options).ConfigureAwait(false);
    }
}
