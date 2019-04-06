using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides a way to register DataLoader instances
    /// </summary>
    public class DataLoaderContext
    {
        private readonly Dictionary<string, IDataLoader> _loaders = new Dictionary<string, IDataLoader>();

        /// <summary>
        /// Add a new data loader if one does not already exist with the provided key
        /// </summary>
        /// <typeparam name="TDataLoader">The type of <seealso cref="IDataLoader"/></typeparam>
        /// <param name="loaderKey">Unique string to identify the <seealso cref="IDataLoader"/> instance</param>
        /// <param name="dataLoaderFactory">Function to create the TDataLoader instance if it does not already exist</param>
        /// <returns>Returns an existing TDataLoader instance or a newly created instance if it did not exist already</returns>
        public TDataLoader GetOrAdd<TDataLoader>(string loaderKey, Func<TDataLoader> dataLoaderFactory)
            where TDataLoader : IDataLoader
        {
            if (loaderKey == null)
                throw new ArgumentNullException(nameof(loaderKey));

            if (dataLoaderFactory == null)
                throw new ArgumentNullException(nameof(dataLoaderFactory));

            IDataLoader loader;

            lock (_loaders)
            {
                if (!_loaders.TryGetValue(loaderKey, out loader))
                {
                    loader = dataLoaderFactory();

                    _loaders.Add(loaderKey, loader);
                }
            }

            return (TDataLoader)loader;
        }

        /// <summary>
        /// Dispatch all registered data loaders
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegates</param>
        public async Task DispatchAllAsync(CancellationToken cancellationToken = default)
        {
            Task task;

            lock (_loaders)
            {
                if (_loaders.Count == 0)
                    return;

                var tasks = new List<Task>(_loaders.Count);

                foreach (var loader in _loaders.Values)
                {
                    tasks.Add(loader.DispatchAsync(cancellationToken));
                }

                task = Task.WhenAll(tasks);
            }

            await task;
        }
    }
}
