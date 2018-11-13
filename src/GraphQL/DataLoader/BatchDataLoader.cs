using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GraphQL.DataLoader
{
    public class BatchDataLoader<TKey, T> : DataLoaderBase<IDictionary<TKey, T>>, IDataLoader<TKey, T>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> _loader;
        private readonly HashSet<TKey> _pendingKeys;
        private readonly Dictionary<TKey, T> _cache;
        private readonly T _defaultValue;

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T),
            ILogger logger = null,
            string loaderKey = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;
            Log = logger;
            LoaderKey = loaderKey;
        }

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T),
            ILogger logger = null,
            string loaderKey = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<IDictionary<TKey, T>> LoadAndMapToDictionary(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                logger?.LogInformation($"BatchDataLoader - before '{LoaderKey}' map awaited");
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                logger?.LogInformation($"BatchDataLoader - after '{LoaderKey}' map awaited");
                var lookup = values.ToDictionary(keySelector, keyComparer);
                logger?.LogInformation($"BatchDataLoader - '{LoaderKey}' lookup created");
                return lookup;
            }

            _loader = LoadAndMapToDictionary;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;
            Log = logger;
            LoaderKey = loaderKey;
        }

        public async Task<T> LoadAsync(TKey key)
        {
            lock (_cache)
            {
                // Get value from the cache if it's there
                if (_cache.TryGetValue(key, out T cacheValue))
                {
                    Log?.LogInformation($"BatchDataLoader - Fetching '{key}' from '{LoaderKey}' cache");
                    return cacheValue;
                }

                // Otherwise add to pending keys
                if (!_pendingKeys.Contains(key))
                {
                    _pendingKeys.Add(key);
                }
            }

            // Log?.LogInformation($"Batch data loader - awaiting {key}");

            var result = await DataLoaded;

            // Log?.LogInformation($"Batch data loader - after await {key}");

            if (result.TryGetValue(key, out T value))
            {
                return value;
            }
            else
            {
                return _defaultValue;
            }
        }

        protected override bool IsFetchNeeded()
        {
            lock (_cache)
            {
                return _pendingKeys.Count > 0;
            }
        }

        protected override async Task<IDictionary<TKey, T>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<TKey> keys;

            lock (_cache)
            {
                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            Log?.LogInformation($"BatchDataLoader - FetchAsync '{LoaderKey}'");

            var dictionary = await _loader(keys, cancellationToken).ConfigureAwait(false);

            Log?.LogInformation($"BatchDataLoader - FetchAsync '{LoaderKey}' awaited");

            // Populate cache
            lock (_cache)
            {
                foreach (TKey key in keys)
                {
                    if (dictionary.TryGetValue(key, out T value))
                    {
                        _cache[key] = value;
                    }
                    else
                    {
                        _cache[key] = _defaultValue;
                    }
                }
            }

            Log?.LogInformation($"BatchDataLoader - '{LoaderKey}' cache populated");

            return dictionary;
        }
    }
}
