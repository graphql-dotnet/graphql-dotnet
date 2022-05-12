using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <summary>
/// A basic implementation of a query cache, limited by a configured amount of memory.
/// </summary>
public class MemoryQueryCache : BaseMemoryCache<string, MemoryQueryCacheOptions>, IQueryCache
{
    /// <inheritdoc cref="BaseMemoryCache{GraphQLDocument, MemoryQueryCacheOptions}.BaseMemoryCache()"/>
    public MemoryQueryCache()
    {
    }

    /// <inheritdoc cref="BaseMemoryCache{GraphQLDocument, MemoryQueryCacheOptions}.BaseMemoryCache(IOptions{MemoryQueryCacheOptions})"/>
    public MemoryQueryCache(IOptions<MemoryQueryCacheOptions> options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    protected override MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string _, string value)
    {
        return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = value.Length };
    }
}
