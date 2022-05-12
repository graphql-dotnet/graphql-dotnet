using Microsoft.Extensions.Caching.Memory;

namespace GraphQL.Caching;

/// <summary>
/// A basic implementation of a query cache, limited by a configured amount of memory.
/// </summary>
public class MemoryQueryCache : BaseMemoryCache<string, MemoryQueryCacheOptions>, IQueryCache
{
    /// <inheritdoc/>
    protected override MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string _, string value)
    {
        return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = value.Length };
    }
}
