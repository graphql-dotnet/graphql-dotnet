using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Caching;

/// <summary>
/// Provides configuration options for <see cref="AutomaticPersistedQueriesExecution"/>.
/// </summary>
public class AutomaticPersistedQueriesCacheOptions : MemoryCacheOptions, IOptions<AutomaticPersistedQueriesCacheOptions>
{
    /// <summary>
    /// Initializes a default instance with the size limit (sum of all requests lengths in symbols) set to 100,000.
    /// </summary>
    public AutomaticPersistedQueriesCacheOptions()
    {
        SizeLimit = 100000;
    }

    /// <summary>
    /// The maximum lifetime of queries cached within this instance. Upon cache hit, the expiration time
    /// for the query is reset to this value. Defaults to <see langword="null"/>, indicating that there is no expiration.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    AutomaticPersistedQueriesCacheOptions IOptions<AutomaticPersistedQueriesCacheOptions>.Value => this;
}
