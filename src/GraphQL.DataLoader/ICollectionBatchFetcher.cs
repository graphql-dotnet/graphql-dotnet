namespace GraphQL.DataLoader;

/// <summary>
/// Defines a collection batch fetcher for loading multiple collections of values asynchronously based on a set of keys.
/// </summary>
/// <typeparam name="TKey">The type of the key used to fetch the collections.</typeparam>
/// <typeparam name="T">The type of the values within the collections to be fetched.</typeparam>
/// <remarks>
/// Implementations of this interface are responsible for defining how data is fetched in batches where each key corresponds to a collection of values.
/// This is useful in scenarios where a single key is associated with multiple values, and fetching data individually for each key-value pair would be inefficient.
/// </remarks>
public interface ICollectionBatchFetcher<TKey, T>
    where TKey : notnull
{
    /// <summary>
    /// Asynchronously fetches collections of values based on the provided keys.
    /// </summary>
    /// <param name="keys">The collection of keys to fetch the value collections for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the fetch operation.</param>
    /// <returns>
    /// A task that represents the asynchronous fetch operation. The task result contains an ILookup mapping each key to its corresponding collection of values.
    /// </returns>
    /// <remarks>
    /// Implementations should handle scenarios where some keys may not correspond to any collection of values.
    /// In cases where keys do not have corresponding collections in the returned ILookup, the data loader will utilize a default collection (possibly empty) for those keys.
    /// This ensures consistent handling of missing data across different implementations of the fetcher.
    /// </remarks>
    Task<ILookup<TKey, T>> FetchAsync(IEnumerable<TKey> keys, CancellationToken token);
}
