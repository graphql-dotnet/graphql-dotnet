using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    public class CollectionBatchDataLoader<TKey, T> : DataLoaderBase<ILookup<TKey, T>>, IDataLoader<TKey, IEnumerable<T>>
    {
        private static readonly ILookup<TKey, T> EmptyResult = Enumerable.Empty<T>().ToLookup(_ => default(TKey));

        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;
        private readonly Dictionary<TKey, IEnumerable<T>> _cache;
        private readonly List<TKey> _pendingKeys = new List<TKey>();

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> loader, IEqualityComparer<TKey> keyComparer = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
        }

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader, Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<ILookup<TKey, T>> LoadAndMapToLookup(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                return values.ToLookup(keySelector, keyComparer);
            }

            _loader = LoadAndMapToLookup;
            _cache = new Dictionary<TKey, IEnumerable<T>>(keyComparer);
        }

        public Task<IEnumerable<T>> LoadAsync(TKey key)
        {
            lock (_cache)
            {
                // Get value from the cache if it's there
                if (_cache.TryGetValue(key, out var value))
                {
                    return Task.FromResult(value);
                }

                // Otherwise add to pending keys
                _pendingKeys.Add(key);

                // Return task which will complete when this loader is dispatched
                return DataLoaded.ContinueWith(task => task.Result[key],
                    TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        protected override async Task<ILookup<TKey, T>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<TKey> keys;

            lock (_cache)
            {
                // If there's nothing to load, return an empty dictionary
                if (_pendingKeys.Count == 0)
                    return EmptyResult;

                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            var lookup = await _loader(keys, cancellationToken).ConfigureAwait(false);

            // Populate cache
            lock (_cache)
            {
                foreach (var group in lookup)
                {
                    _cache[group.Key] = group;
                }
            }

            return lookup;
        }
    }
}
