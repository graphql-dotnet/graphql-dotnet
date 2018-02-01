using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class SimpleDataLoader<T> : DataLoaderBase<T>, IDataLoader<T>
    {
        private readonly object _lock = new object();
        private readonly Func<CancellationToken, Task<T>> _loader;

        private Task<T> _cachedTask;

        public SimpleDataLoader(Func<CancellationToken, Task<T>> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public Task<T> LoadAsync()
        {
            // Return the cached task if we have one
            if (_cachedTask != null)
                return _cachedTask;

            lock (_lock)
            {
                return _cachedTask ?? DataLoaded;
            }
        }

        protected override bool IsFetchNeeded()
        {
            lock (_lock)
            {
                // No need to re-fetch if we have a cached task
                return _cachedTask == null;
            }
        }

        protected override Task<T> FetchAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                // Cache the task
                _cachedTask = _loader(cancellationToken);
            }

            return _cachedTask;
        }
    }
}
