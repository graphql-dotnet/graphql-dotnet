using System;
using GraphQL.Language.AST;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    /// <summary>
    /// A basic implementation of a document cache, limited by a configured amount of memory.
    /// </summary>
    public class MsMemoryDocumentCache : IDocumentCache, IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly TimeSpan _slidingExpiration;

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="maxTotalQueryLength">The total length of all queries cached in this instance. Assume maximum memory used is about 10x this value. Will not cache queries larger than 1/3 of this value. During cache compression, reduces cache size by 1/3 of this value.</param>
        /// <param name="slidingExpiration">The maximum lifetime of queries cached within this instance.</param>
        public MsMemoryDocumentCache(int maxTotalQueryLength, TimeSpan slidingExpiration)
        {
            if (maxTotalQueryLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTotalQueryLength));
            if (slidingExpiration.Ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(slidingExpiration));
            _memoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = maxTotalQueryLength });
            _slidingExpiration = slidingExpiration;
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="options">The memory cache configuration settings. Note that the <see cref="MemoryCacheOptions.SizeLimit"/> field is required.</param>
        /// <param name="slidingExpiration">The maximum lifetime of queries cached within this instance.</param>
        public MsMemoryDocumentCache(IOptions<MemoryCacheOptions> options, TimeSpan slidingExpiration)
        {
            _memoryCache = new MemoryCache(options ?? throw new ArgumentNullException(nameof(options)));
            if (slidingExpiration.Ticks <= 0)
                throw new ArgumentOutOfRangeException(nameof(slidingExpiration));
            _slidingExpiration = slidingExpiration;
        }

        /// <inheritdoc/>
        public Document this[string query]
        {
            get => _memoryCache.TryGetValue<Document>(query, out var value) ? value : null;
            set => _memoryCache.Set(query ?? throw new ArgumentNullException(nameof(query)), value, new MemoryCacheEntryOptions { SlidingExpiration = _slidingExpiration, Size = query.Length });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _memoryCache.Dispose();
        }
    }

}
