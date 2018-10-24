using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GraphQL.DataLoader
{
    public class CollectionBatchDataLoader<TKey, T> : DataLoaderBase<ILookup<TKey, T>>, IDataLoader<TKey, IEnumerable<T>>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;
        private readonly Dictionary<TKey, IEnumerable<T>> _cache;
        private readonly HashSet<TKey> _pendingKeys;

        public CollectionBatchDataLoader(
            Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null,
            ILogger logger = null,
            string loaderKey = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
            _pendingKeys = new HashSet<TKey>(keyComparer);
            Log = logger;
            LoaderKey = loaderKey;
        }

        public CollectionBatchDataLoader(
            Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            ILogger logger = null,
            string loaderKey = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<ILookup<TKey, T>> LoadAndMapToLookup(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                logger?.LogInformation($"CollectionBatchDataLoader - before '{LoaderKey}' map awaited");
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                logger?.LogInformation($"CollectionBatchDataLoader - after '{LoaderKey}' map awaited");
                var lookup = values.ToLookup(keySelector, keyComparer);
                logger?.LogInformation($"CollectionBatchDataLoader - '{LoaderKey}' lookup created");
                return lookup;
            }

            _loader = LoadAndMapToLookup;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
            _pendingKeys = new HashSet<TKey>(keyComparer);
            Log = logger;
            LoaderKey = loaderKey;
        }

        public async Task<IEnumerable<T>> LoadAsync(TKey key)
        {
            lock (_cache)
            {
                // Get value from the cache if it's there
                if (_cache.TryGetValue(key, out var value))
                {
                    return value;
                }

                // Otherwise add to pending keys
                if (!_pendingKeys.Contains(key))
                {
                    _pendingKeys.Add(key);
                }
            }

            var result = await DataLoaded;

            return result[key];
        }

        protected override bool IsFetchNeeded()
        {
            lock (_cache)
            {
                return _pendingKeys.Count > 0;
            }
        }

        protected override async Task<ILookup<TKey, T>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<TKey> keys;

            lock (_cache)
            {
                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            Log?.LogInformation($"CollectionBatchDataLoader - FetchAsync '{LoaderKey}'");

            var lookup = await _loader(keys, cancellationToken).ConfigureAwait(false);

            Log?.LogInformation($"CollectionBatchDataLoader - FetchAsync '{LoaderKey}' awaited");

            // Populate cache
            lock (_cache)
            {
                foreach (TKey key in keys)
                {
                    _cache[key] = lookup[key].ToArray();
                }
            }

            Log?.LogInformation($"CollectionBatchDataLoader - '{LoaderKey}' cache populated");

            return lookup;
        }
    }
}
