using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class BatchDataLoader<TKey, T> : DataLoaderBase<IDictionary<TKey, T>>, IDataLoader<TKey, T>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> _loader;
        private readonly HashSet<TKey> _pendingKeys;
        private readonly Dictionary<TKey, T> _cache;
        private readonly T _defaultValue;
        private readonly int? _maxBatchSize;

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T),
            int? maxBatchSize = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;

            if(maxBatchSize.HasValue && maxBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Has to be greater than zero.");

            _maxBatchSize = maxBatchSize;
        }

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T),
            int? maxBatchSize = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (maxBatchSize.HasValue && maxBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Has to be greater than zero.");

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<IDictionary<TKey, T>> LoadAndMapToDictionary(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
               return (await loader(keys, cancellationToken).ConfigureAwait(false)).ToDictionary(keySelector, keyComparer);
            }

            _loader = LoadAndMapToDictionary;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _cache = new Dictionary<TKey, T>(keyComparer);
            _defaultValue = defaultValue;
            _maxBatchSize = maxBatchSize;
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

        protected override async Task<IDictionary<TKey, T>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<TKey> keys;

            lock (_cache)
            {
                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            IDictionary<TKey, T> dictionary;

            if (!_maxBatchSize.HasValue || _maxBatchSize.Value >= keys.Count)
                dictionary = await _loader(keys, cancellationToken).ConfigureAwait(false);
            else
            {
                dictionary = new Dictionary<TKey, T>();

                foreach (var batch in Batch(keys, _maxBatchSize.Value))
                {
                    dictionary = dictionary.Concat(await _loader(batch, cancellationToken).ConfigureAwait(false)).ToDictionary(x => x.Key, x => x.Value);
                }
            }
            

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
