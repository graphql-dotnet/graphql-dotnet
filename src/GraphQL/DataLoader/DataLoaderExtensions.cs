using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public static class DataLoaderExtensions
    {
        /// <summary>
        /// Asynchronously load data for the provided given keys
        /// </summary>
        /// <param name="dataLoader">The dataloader to use</param>
        /// <param name="keys">Keys to use for loading data</param>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        public static Task<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, IEnumerable<TKey> keys)
        {
            var tasks = new List<Task<T>>(keys.Count());

            foreach (var key in keys)
            {
                tasks.Add(dataLoader.LoadAsync(key));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Asynchronously load data for the provided given keys
        /// </summary>
        /// <param name="dataLoader">The dataloader to use</param>
        /// <param name="keys">Keys to use for loading data</param>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        public static Task<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, params TKey[] keys)
        {
            return dataLoader.LoadAsync(keys.AsEnumerable());
        }
    }
}
