using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <summary>
/// Provides configuration options for <see cref="MemoryDocumentCache"/>.
/// </summary>
public class MemoryDocumentCacheOptions : MemoryCacheOptions, IOptions<MemoryDocumentCacheOptions>
{
    /// <summary>
    /// Initializes a default instance with the size limit set to 100,000.
    /// </summary>
    public MemoryDocumentCacheOptions()
    {
        SizeLimit = 100000;
    }

    /// <summary>
    /// The maximum lifetime of queries cached within this instance. Upon cache hit, the expiration time
    /// for the query is reset to this value. Defaults to <see langword="null"/>, indicating that there is no expiration.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// A delegate that supplies an extra value to be included in the cache key.
    /// By default, this returns the <see cref="ExecutionOptions.Schema"/>. You can override
    /// this behavior to incorporate additional uniqueness.
    /// </summary>
    public Func<ExecutionOptions, object?>? AdditionalCacheKeySelector { get; set; }

    MemoryDocumentCacheOptions IOptions<MemoryDocumentCacheOptions>.Value => this;
}
