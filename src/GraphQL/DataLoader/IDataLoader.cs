using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides a method to dispatch pending a operation to load data.
    /// </summary>
    public interface IDataLoader
    {
        /// <summary>
        /// Dispatch any pending operations
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        void Dispatch(CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Provides a method of queueing a data loading operation to be dispatched later.
    /// </summary>
    /// <typeparam name="T">The type of data to be loaded</typeparam>
    public interface IDataLoader<T>
    {
        /// <summary>
        /// Asynchronously load data
        /// </summary>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        Task<T> LoadAsync();
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
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        Task<T> LoadAsync(TKey key);
    }
}
