namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides a method to dispatch a pending operation to load data.
    /// </summary>
    public interface IDataLoader
    {
        /// <summary>
        /// Dispatch any pending operations
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to the fetch delegate</param>
        Task DispatchAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides a method of queuing a data loading operation to be dispatched later.
    /// </summary>
    /// <typeparam name="T">The type of data to be loaded</typeparam>
    public interface IDataLoader<T>
    {
        /// <summary>
        /// Asynchronously load data
        /// </summary>
        /// <returns>
        /// An object representing a pending operation.
        /// </returns>
        IDataLoaderResult<T> LoadAsync();
    }

    /// <summary>
    /// Provides a method of queueing a data loading operation to be dispatched later.
    /// </summary>
    /// <typeparam name="TKey">The type of key to use to load data</typeparam>
    /// <typeparam name="T">The type of data to be loaded</typeparam>
    public interface IDataLoader<TKey, T>
    {
        /// <summary>
        /// Asynchronously load data for the provided given key
        /// </summary>
        /// <param name="key">Key to use for loading data</param>
        /// <returns>
        /// An object representing a pending operation
        /// </returns>
        IDataLoaderResult<T> LoadAsync(TKey key);
    }
}
