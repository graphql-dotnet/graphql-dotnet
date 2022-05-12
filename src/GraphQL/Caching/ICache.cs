namespace GraphQL.Caching;

/// <summary>
/// Represents an arbitrary cache.
/// </summary>
public interface ICache<TValue>
{
    /// <summary>
    /// Gets a value in the cache. Must be thread-safe.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The cached value. Returns <see langword="null"/> if no value is found.</returns>
    ValueTask<TValue?> GetAsync(string key);

    /// <summary>
    /// Sets a value in the cache. Must be thread-safe.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value to cache.</param>
    ValueTask SetAsync(string key, TValue value);
}
