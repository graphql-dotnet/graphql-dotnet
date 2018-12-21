using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class CollectionBatchDataLoader<TKey, T> : DataLoaderBase<ILookup<TKey, T>>, IDataLoader<TKey, IEnumerable<T>>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;
        private readonly Dictionary<TKey, IEnumerable<T>> _cache;
        private readonly HashSet<TKey> _pendingKeys;
        private readonly int? _maxBatchSize;

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> loader, IEqualityComparer<TKey> keyComparer = null, int? maxBatchSize = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
            _pendingKeys = new HashSet<TKey>(keyComparer);

            if (maxBatchSize.HasValue && maxBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Has to be greater than zero.");
            _maxBatchSize = maxBatchSize;
        }

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader, Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null, int? maxBatchSize = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (maxBatchSize.HasValue && maxBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Has to be greater than zero.");

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _maxBatchSize = maxBatchSize;

            async Task<ILookup<TKey, T>> LoadAndMapToLookup(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                return values.ToLookup(keySelector, keyComparer);
            }

            _loader = LoadAndMapToLookup;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
            _pendingKeys = new HashSet<TKey>(keyComparer);
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

            ILookup<TKey, T> lookup;

            if(!_maxBatchSize.HasValue || _maxBatchSize.Value >= keys.Count)
                lookup = await _loader(keys, cancellationToken).ConfigureAwait(false);
            else
            {
                lookup = Enumerable.Empty<T>().ToLookup(x => default(TKey));

                foreach (var batch in Batch(keys, _maxBatchSize.Value))
                {
                    var batchLookup = await _loader(batch, cancellationToken).ConfigureAwait(false);
                    lookup = lookup.Concat(batchLookup).SelectMany(x => x.Select(value => new {x.Key, value})).ToLookup(x => x.Key, x => x.value);
                }
            }

            // Populate cache
            lock (_cache)
            {
                foreach (TKey key in keys)
                {
                    _cache[key] = lookup[key].ToArray();
                }
            }

            return lookup;
        }
    }
}
