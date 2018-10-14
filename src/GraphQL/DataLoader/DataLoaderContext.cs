using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides a way to register DataLoader instances
    /// </summary>
    public class DataLoaderContext
    {
        private readonly ConcurrentDictionary<string, IDataLoader> _loaders = new ConcurrentDictionary<string, IDataLoader>();
        private Queue<IDataLoader> _queue = new Queue<IDataLoader>();

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

            var loader = _loaders.GetOrAdd(loaderKey, key =>
            {
                var newLoader = dataLoaderFactory();
                _queue.Enqueue(newLoader);
                return newLoader;
            });

            return (TDataLoader)loader;
        }

        /// <summary>
        /// Dispatch all registered data loaders
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        public async Task DispatchAllAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            while (tasks.Any() || _queue.Any())
            {
                if (_queue.Any())
                {
                    var queue = Interlocked.Exchange(ref _queue, new Queue<IDataLoader>());
                    while (queue.Any()) tasks.Add(await queue.Dequeue().DispatchAsync(cancellationToken).ConfigureAwait(false));
                }

                tasks.Remove(await Task.WhenAny(tasks).ConfigureAwait(false));
            }
        }
    }
}
