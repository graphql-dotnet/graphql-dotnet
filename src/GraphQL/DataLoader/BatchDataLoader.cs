using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class BatchDataLoader<TKey, T> : DataLoaderBase<Dictionary<TKey, T>>, IDataLoader<TKey, T>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<Dictionary<TKey, T>>> _loader;
        private readonly HashSet<TKey> _pendingKeys;
        private readonly Dictionary<TKey, T> _cache;
        private readonly T _defaultValue;

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<Dictionary<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T))
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;
        }

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T))
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<Dictionary<TKey, T>> LoadAndMapToDictionary(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                return values.ToDictionary(keySelector, keyComparer);
            }

            _loader = LoadAndMapToDictionary;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;
        }

        public async Task<T> LoadAsync(TKey key)
        {
            lock (_cache)
            {
                // Get value from the cache if it's there
                if (_cache.TryGetValue(key, out T cacheValue))
                {
                    return cacheValue;
                }

                // Otherwise add to pending keys
                if (!_pendingKeys.Contains(key))
                {
                    _pendingKeys.Add(key);
                }
            }

            var result = await DataLoaded;

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

        protected override async Task<Dictionary<TKey, T>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<TKey> keys;

            lock (_cache)
            {
                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            var dictionary = await _loader(keys, cancellationToken).ConfigureAwait(false);

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

            return dictionary;
        }
    }
}
