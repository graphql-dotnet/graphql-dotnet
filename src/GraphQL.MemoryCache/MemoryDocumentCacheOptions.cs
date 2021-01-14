using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching
{
    /// <summary>
    /// Provides configuration options for <see cref="MemoryDocumentCache"/>.
    /// </summary>
    public class MemoryDocumentCacheOptions : MemoryCacheOptions, IOptions<MemoryDocumentCacheOptions>
    {
        public MemoryDocumentCacheOptions()
        {
            SizeLimit = 100000;
        }
        /// <summary>
        /// The maximum total length of all queries cached. Assume maximum memory used is about 10x this value. Defaults to 100,000.
        /// </summary>
        public long? MaxTotalQueryLength { get; set; } = 100000;

        /// <summary>
        /// The maximum lifetime of queries cached within this instance. Upon cache hit, the expiration time
        /// for the query is reset to this value. Defaults to null, indicating that there is no expiration.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        MemoryDocumentCacheOptions IOptions<MemoryDocumentCacheOptions>.Value => this;
    }
}
