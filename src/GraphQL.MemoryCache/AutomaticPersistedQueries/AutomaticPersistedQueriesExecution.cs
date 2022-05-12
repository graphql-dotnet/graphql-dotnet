using System.Security.Cryptography;
using System.Text;
using GraphQL.DI;

namespace GraphQL.Caching;

/// <summary>
/// Implementation of Automatic Persisted Queries.
/// https://www.apollographql.com/docs/react/api/link/persisted-queries/
/// </summary>
public class AutomaticPersistedQueriesExecution : IConfigureExecution
{
    private readonly IQueryCache _cache;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public AutomaticPersistedQueriesExecution(IQueryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Searching a hash in <see cref="ExecutionOptions.Extensions"/> based on a protocol:
    /// https://www.apollographql.com/docs/react/api/link/persisted-queries/#protocol
    /// </summary>
    protected virtual string? GetHash(Inputs? extensions)
    {
        if (
            (extensions?.TryGetValue("persistedQuery", out var persistedQueryObject) ?? false) &&
            persistedQueryObject is Dictionary<string, object> persistedQuery &&
            persistedQuery.TryGetValue("sha256Hash", out var sha256HashObject) &&
            sha256HashObject is string sha256Hash &&
            !string.IsNullOrWhiteSpace(sha256Hash))
        {
            return sha256Hash;
        }

        return null;
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
    /// Create <see cref="ExecutionResult"/> with specific error.
    /// </summary>
    protected virtual ExecutionResult CreateExecutionResult(ExecutionError error) => new ExecutionResult { Errors = new ExecutionErrors { error } };

    /// <inheritdoc/>
    public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
    {
        var hash = GetHash(options.Extensions);

        if (string.IsNullOrWhiteSpace(options.Query))
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return CreateExecutionResult(new ExecutionError("GraphQL query is missing."));
            }
            else
            {
                var queryFromCache = await _cache.GetAsync(hash!).ConfigureAwait(false);

                if (queryFromCache == null)
                {
                    return CreateExecutionResult(new PersistedQueryNotFoundError(hash!));
                }
                else
                {
                    options.Query = queryFromCache;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(hash))
        {
            if (hash!.Equals(ComputeSHA256(options.Query!), StringComparison.InvariantCultureIgnoreCase))
            {
                await _cache.SetAsync(hash!, options.Query!).ConfigureAwait(false);
            }
            else
            {
                return CreateExecutionResult(new PersistedQueryBadHashError(hash));
            }
        }

        return await next(options).ConfigureAwait(false);
    }
}
