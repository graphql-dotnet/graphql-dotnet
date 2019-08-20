using System;
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
        private TaskCompletionSource<bool> _dispatchNeededSource = new TaskCompletionSource<bool>();
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
                    // Listen for DispatchNeeded on the added dataloader
                    loader.DispatchNeeded.ContinueWith(t =>
                    {
                        _dispatchNeededSource.TrySetResult(true);
                        _dispatchNeededSource = new TaskCompletionSource<bool>();
                    });
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

        /// <summary>
        /// A task that complets when a DataLoader is in need of dispatch.
        /// </summary>
        public Task DispatchNeeded
        {
            get
            {
                if (_queue.Count > 0)
                {
                    // Return a task the completes when any of these two conditions are met:
                    // 1. Any registered DataLoaders at this time is in need of dispatch
                    // 2. A DataLoader added after this task is retrieved (Using GetOrAdd) is in need of dispatch
                    var existingLoaders = (_queue.Count == 1) ? _queue.Peek().DispatchNeeded :
                        Task.WhenAny(_queue.Select(loader => loader.DispatchNeeded));

                    return Task.WhenAny(existingLoaders, _dispatchNeededSource.Task);
                }
                // Now dataloaders registered yet, so only return the _dispatchNeededSource which
                // is triggered if any dataloaders are added after this point in time is in need of dispatch
                return _dispatchNeededSource.Task;

            }
        }
    }
}
