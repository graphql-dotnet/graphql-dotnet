using GraphQLParser.AST;
using Microsoft.Extensions.Caching.Memory;

namespace GraphQL.Caching;

/// <summary>
/// A basic implementation of a document cache, limited by a configured amount of memory.
/// </summary>
public class MemoryDocumentCache : BaseMemoryCache<GraphQLDocument, MemoryDocumentCacheOptions>, IDocumentCache
{
    /// <inheritdoc/>
    protected override MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string key, GraphQLDocument _)
    {
        return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration, Size = key.Length };
    }
}
