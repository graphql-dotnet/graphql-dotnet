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

    private readonly object _lockObject = new();
    private readonly SHA256 _sha256 = SHA256.Create();

    /// <summary>
    /// Searching APQ fields in <see cref="ExecutionOptions.Extensions"/> based on a protocol:
    /// https://www.apollographql.com/docs/react/api/link/persisted-queries/#protocol
    /// </summary>
    public virtual (string? Hash, string? Version, bool Enabled) GetAPQProperties(Inputs? extensions)
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
    /// Compute SHA256 hash.
    /// </summary>
    public virtual string ComputeSHA256(string input)
    {
        byte[] bytes;
        lock (_lockObject)
        {
            bytes = _sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

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
    public virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new ExecutionResult { Errors = new ExecutionErrors { error } };

    /// <summary>
    /// Get query by hash. It is likely to be implemented via cache mechanism.
    /// </summary>
    public abstract ValueTask<string?> GetQueryAsync(string hash);

    /// <summary>
    /// Set query by hash. It is likely to be implemented via cache mechanism.
    /// </summary>
    public abstract ValueTask SetQueryAsync(string hash, string query);

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
            if (apq.Hash.Equals(ComputeSHA256(options.Query), StringComparison.InvariantCultureIgnoreCase))
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
