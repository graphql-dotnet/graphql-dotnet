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
        private readonly Queue<IDataLoader> _queue = new Queue<IDataLoader>();

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
                    _queue.Enqueue(loader);
                }
            }

            return (TDataLoader)loader;
        }

        /// <summary>
        /// Dispatch all registered data loaders
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        public async Task DispatchAllAsync(CancellationToken cancellationToken = default)
        {
            Task task;

            lock (_loaders)
            {
                if (_queue.Count == 0)
                {
                    return;
                }
                else if (_queue.Count == 1)
                {
                    var loader = _queue.Peek();
                    task = loader.DispatchAsync(cancellationToken);
                }
                else
                {
                    var tasks = new List<Task>(_queue.Count);

                    // We don't want to pop any loaders off the queue because they may get more work later
                    foreach (var loader in _queue)
                    {
                        tasks.Add(loader.DispatchAsync(cancellationToken));
                    }

                    task = Task.WhenAll(tasks);
                }
            }

            await task.ConfigureAwait(false);
        }
    }
}
