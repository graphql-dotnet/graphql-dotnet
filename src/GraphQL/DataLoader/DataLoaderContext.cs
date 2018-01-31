using System;
using System.Collections.Generic;
using System.Threading;

namespace GraphQL.DataLoader
{
    public class DataLoaderContext
    {
        private readonly Dictionary<string, IDataLoader> _loaders = new Dictionary<string, IDataLoader>();
        private readonly Queue<IDataLoader> _queue = new Queue<IDataLoader>();

        /// <summary>
        /// Add a new data loader if one does not already exist with the provided key
        /// </summary>
        /// <typeparam name="TDataLoader"></typeparam>
        /// <param name="loaderKey"></param>
        /// <param name="dataLoaderFactory"></param>
        /// <returns></returns>
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
        /// Dispatch all queued data loaders
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void DispatchAll(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_loaders)
            {
                // We don't want to pull any loaders off the queue because they may get more work later
                foreach (var loader in _queue)
                {
                    loader.Dispatch(cancellationToken);
                }
            }
        }
    }
}
