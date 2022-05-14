using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <inheritdoc/>
public class AutomaticPersistedQueriesExecution : AutomaticPersistedQueriesExecutionBase, IDisposable
{
    private readonly AutomaticPersistedQueriesCacheOptions _options;
    private readonly MemoryCache _cache;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public AutomaticPersistedQueriesExecution()
        : this(new AutomaticPersistedQueriesCacheOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public AutomaticPersistedQueriesExecution(IOptions<AutomaticPersistedQueriesCacheOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = new MemoryCache(options);
    }

    /// <inheritdoc/>
    protected override ValueTask<string?> GetQueryAsync(string hash) => new(_cache.TryGetValue<string>(hash, out var value) ? value : null);

    /// <inheritdoc/>
    protected override Task SetQueryAsync(string hash, string query)
    {
        _cache.Set(hash, query, new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = query.Length });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _cache.Dispose();
}
