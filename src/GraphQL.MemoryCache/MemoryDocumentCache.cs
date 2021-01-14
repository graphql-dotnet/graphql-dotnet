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
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="maxTotalQueryLength">The total length of all queries cached in this instance. Assume maximum memory used is about 10x this value. Will not cache queries larger than 1/3 of this value. During cache compression, reduces cache size by 1/3 of this value.</param>
        /// <param name="slidingExpiration">The maximum lifetime of queries cached within this instance.</param>
        public MemoryDocumentCache(int maxTotalQueryLength, TimeSpan slidingExpiration)
            : this(
                maxTotalQueryLength <= 0
                    ? throw new ArgumentOutOfRangeException(nameof(maxTotalQueryLength))
                    : new MemoryCache(new MemoryCacheOptions { SizeLimit = maxTotalQueryLength }),
                slidingExpiration)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified memory cache and sliding expiration time period.
        /// Note that by overriding <see cref="GetMemoryCacheEntryOptions(string)"/>, the sliding expiration time specified here can be ignored.
        /// </summary>
        protected MemoryDocumentCache(IMemoryCache memoryCache, TimeSpan slidingExpiration)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _slidingExpiration = slidingExpiration;
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="options">The memory cache configuration settings. Note that the <see cref="MemoryCacheOptions.SizeLimit"/> field is required.</param>
        /// <param name="slidingExpiration">The maximum lifetime of queries cached within this instance.</param>
        public MemoryDocumentCache(IOptions<MemoryCacheOptions> options, TimeSpan slidingExpiration)
            : this(
                new MemoryCache(options ?? throw new ArgumentNullException(nameof(options))),
                slidingExpiration)
        {
        }

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
