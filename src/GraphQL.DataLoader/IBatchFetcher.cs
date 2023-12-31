namespace GraphQL.DataLoader;

/// <summary>
/// Defines a batch fetcher for loading multiple values asynchronously based on a set of keys.
/// </summary>
/// <typeparam name="TKey">The type of the key used to fetch the values.</typeparam>
/// <typeparam name="T">The type of the values to be fetched.</typeparam>
/// <remarks>
/// Implementations of this interface are responsible for defining how data is fetched in batches.
/// This is typically used in scenarios where fetching data individually for each key would be inefficient.
/// </remarks>
public interface IBatchFetcher<TKey, T>
    where TKey : notnull
{
    /// <summary>
    /// Asynchronously fetches a collection of values based on the provided keys.
    /// </summary>
    /// <param name="keys">The collection of keys to fetch the values for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the fetch operation.</param>
    /// <returns>
    /// A task that represents the asynchronous fetch operation. The task result contains a dictionary
    /// mapping each key to its corresponding value.
    /// </returns>
    /// <remarks>
    /// Implementations should handle scenarios where some keys may not correspond to any value.
    /// In cases where keys do not have corresponding values in the returned dictionary, the data loader
    /// will utilize the configured default value for those keys. This ensures consistent handling of missing
    /// data across different implementations of the fetcher.
    /// </remarks>
    Task<IDictionary<TKey, T>> FetchAsync(IEnumerable<TKey> keys, CancellationToken token);
}
