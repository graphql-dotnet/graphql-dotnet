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
        private readonly Dictionary<string, IDataLoader> _loaders = new Dictionary<string, IDataLoader>();
        private TaskCompletionSource<bool> _dispatchingFinishedEvent = new TaskCompletionSource<bool>(false);
        private CancellationTokenSource _dispatchingCancelationTokenSource;

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
        /// Starts dispatching all registered data loaders
        /// </summary>
        public async void StartDispatching()
        {
            _dispatchingCancelationTokenSource = new CancellationTokenSource();
            try
            {
                while (!_dispatchingCancelationTokenSource.IsCancellationRequested)
                {
                    // This busy wait should probably be changed to wait for enqueued load requests (requires refactoring)
                    await Task.Delay(1, _dispatchingCancelationTokenSource.Token);
                    await DispatchAllAsync(_dispatchingCancelationTokenSource.Token);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _dispatchingCancelationTokenSource.Token)
            {
            }
            catch (Exception ex)
            {
                _dispatchingFinishedEvent.SetException(ex);
                return;
            }

            _dispatchingFinishedEvent.SetResult(true);
        }

        /// <summary>
        /// Stops dispatching all registered data loaders
        /// </summary>
        /// <returns></returns>
        public Task StopDispatching()
        {
            _dispatchingFinishedEvent = new TaskCompletionSource<bool>();
            _dispatchingCancelationTokenSource.Cancel();
            return _dispatchingFinishedEvent.Task;
        }

        private async Task DispatchAllAsync(CancellationToken cancellationToken)
        {
            Task task;
            lock (_loaders)
            {
                task = Task.WhenAll(_loaders.Values.Select(x => x.DispatchAsync(cancellationToken)));
            }

            await task.ConfigureAwait(false);
        }
    }
}
