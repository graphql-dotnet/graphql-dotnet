using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    /// <summary>
    /// Provides basic configuration options for <see cref="BaseMemoryCache{TValue,TOptions}"/>.
    /// </summary>
    public abstract class BaseMemoryCacheOptions<TDescendantOptions> : MemoryCacheOptions, IOptions<TDescendantOptions>
        where TDescendantOptions : BaseMemoryCacheOptions<TDescendantOptions>, new()
    {
        /// <summary>
        /// Initializes a default instance with the size limit set to 100,000.
        /// </summary>
        public BaseMemoryCacheOptions()
        {
            SizeLimit = 100000;
        }

        /// <summary>
        /// The maximum lifetime of queries cached within this instance. Upon cache hit, the expiration time
        /// for the query is reset to this value. Defaults to <see langword="null"/>, indicating that there is no expiration.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        TDescendantOptions IOptions<TDescendantOptions>.Value => (this as TDescendantOptions)!;
    }
}
