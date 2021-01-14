using System;
using GraphQL.Language.AST;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    /// <summary>
    /// A basic implementation of a document cache, limited by a configured amount of memory.
    /// </summary>
    public class MemoryDocumentCache : IDocumentCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _slidingExpiration;

        /// <summary>
        /// Initializes a new instance with the specified options.
        /// </summary>
        /// <param name="options">A value containing the <see cref="MemoryDocumentCacheOptions"/> to use.</param>
        public MemoryDocumentCache(IOptions<MemoryDocumentCacheOptions> options)
            : this(
                options.Value.MaxTotalQueryLength <= 0
                  ? throw new ArgumentOutOfRangeException(nameof(options) + "." + nameof(MemoryDocumentCacheOptions.MaxTotalQueryLength))
                  : new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Value.MaxTotalQueryLength }),
                options.Value.SlidingExpiration.Ticks <= 0
                  ? throw new ArgumentOutOfRangeException(nameof(options) + "." + nameof(MemoryDocumentCacheOptions.SlidingExpiration))
                  : options)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified memory cache and sliding expiration time period.
        /// Note that by overriding <see cref="GetMemoryCacheEntryOptions(string)"/>, the sliding expiration
        /// time specified within <paramref name="options"/> can be ignored.
        /// </summary>
        protected MemoryDocumentCache(IMemoryCache memoryCache, IOptions<MemoryDocumentCacheOptions> options)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _slidingExpiration = options?.Value.SlidingExpiration ?? default;
        }

        /// <summary>
        /// Returns a <see cref="MemoryCacheEntryOptions"/> instance for the specified query.
        /// Defaults to setting the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> value as specified
        /// in the constructor, and the <see cref="MemoryCacheEntryOptions.Size"/> value to the length of the query.
        /// </summary>
        protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string query)
        {
            return new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration, Size = query.Length };
        }

        /// <inheritdoc/>
        public virtual Document this[string query]
        {
            get => _memoryCache.TryGetValue<Document>(query, out var value) ? value : null;
            set => _memoryCache.Set(query ?? throw new ArgumentNullException(nameof(query)), value, GetMemoryCacheEntryOptions(query));
        }

        /// <summary>
        /// Disposes of the underlying <see cref="IMemoryCache"/> instance.
        /// </summary>
        public virtual void Dispose()
        {
            _memoryCache.Dispose();
        }
    }
}
