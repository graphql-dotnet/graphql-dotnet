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
        private readonly List<TKey> _pendingKeys = new List<TKey>();
        private readonly Dictionary<TKey, T> _cache;

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<Dictionary<TKey, T>>> loader, IEqualityComparer<TKey> keyComparer = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _cache = new Dictionary<TKey, T>(keyComparer);
        }

        public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader, Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
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
            _cache = new Dictionary<TKey, T>(keyComparer);
        }

        public Task<T> LoadAsync(TKey key)
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
            }

            // Return task which will complete when this loader is dispatched
            return DataLoaded.ContinueWith(task => task.Result[key],
                TaskContinuationOptions.OnlyOnRanToCompletion);
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
                foreach (var kvp in dictionary)
                {
                    _cache[kvp.Key] = kvp.Value;
                }
            }

            return dictionary;
        }
    }
}
