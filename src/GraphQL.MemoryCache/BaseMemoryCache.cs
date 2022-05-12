using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    /// <summary>
    /// A basic implementation of an in memory cache, limited by a configured amount of memory.
    /// </summary>
    public class BaseMemoryCache<TValue, TOptions> : ICache<TValue>, IDisposable
        where TOptions : BaseMemoryCacheOptions<TOptions>, new()
    {
        private readonly IMemoryCache _memoryCache;
        private readonly bool _memoryCacheIsOwned;

        /// <summary>
        /// Cache options.
        /// </summary>
        protected readonly BaseMemoryCacheOptions<TOptions> _options;

        /// <summary>
        /// Initializes a new instance with the default options: 100,000 maximum total query size and no expiration time.
        /// Anticipate memory use of approximately 1MB.
        /// </summary>
        public BaseMemoryCache()
            : this(new TOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified options. Set the <see cref="MemoryCacheOptions.SizeLimit"/>
        /// value to the maximum total query size to be cached. Anticipate about 10x maximum memory use above that value.
        /// </summary>
        /// <param name="options">A value containing the <typeparamref name="TOptions"/> to use.</param>
        public BaseMemoryCache(IOptions<TOptions> options)
            : this(
                new MemoryCache(options),
                true,
                options)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified memory cache and options.
        /// Note that by overriding <see cref="GetMemoryCacheEntryOptions(string, TValue)"/>, the sliding expiration
        /// time specified within <paramref name="options"/> can be ignored.
        /// </summary>
        /// <param name="memoryCache">The memory cache instance to use.</param>
        /// <param name="disposeMemoryCache">Indicates if the memory cache is disposed when this instance is disposed.</param>
        /// <param name="options">Provides option values for use by <see cref="GetMemoryCacheEntryOptions(string, TValue)"/>; optional.</param>
        protected BaseMemoryCache(IMemoryCache memoryCache, bool disposeMemoryCache, IOptions<TOptions> options)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            _memoryCacheIsOwned = disposeMemoryCache;
        }

        /// <summary>
        /// Returns a <see cref="MemoryCacheEntryOptions"/> instance for the specified query.
        /// Defaults to setting the <see cref="MemoryCacheEntryOptions.SlidingExpiration"/> value as specified
        /// in options, and the <see cref="MemoryCacheEntryOptions.Size"/> value to the length of the query.
        /// </summary>
        protected virtual MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string key, TValue value)
        {
            return new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration };
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            if (_memoryCacheIsOwned)
                _memoryCache.Dispose();
        }

        /// <inheritdoc/>
        public virtual ValueTask<TValue?> GetAsync(string key)
        {
            if (_memoryCache.TryGetValue<TValue>(key, out var value))
            {
                return new ValueTask<TValue?>(value);
            }

            return default;
        }

        /// <inheritdoc/>
        public virtual ValueTask SetAsync(string key, TValue value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _memoryCache.Set(key, value, GetMemoryCacheEntryOptions(key, value));

            return default;
        }
    }
}
