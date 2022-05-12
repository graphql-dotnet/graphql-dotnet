using GraphQLParser.AST;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <summary>
/// A basic implementation of a document cache, limited by a configured amount of memory.
/// </summary>
public class MemoryDocumentCache : BaseMemoryCache<GraphQLDocument, MemoryDocumentCacheOptions>, IDocumentCache
{
    /// <inheritdoc cref="BaseMemoryCache{GraphQLDocument, MemoryDocumentCacheOptions}.BaseMemoryCache()"/>
    public MemoryDocumentCache()
    {
    }

    /// <inheritdoc cref="BaseMemoryCache{GraphQLDocument, MemoryDocumentCacheOptions}.BaseMemoryCache(IOptions{MemoryDocumentCacheOptions})"/>
    public MemoryDocumentCache(IOptions<MemoryDocumentCacheOptions> options)
        : base(options)
    {
    }
}
