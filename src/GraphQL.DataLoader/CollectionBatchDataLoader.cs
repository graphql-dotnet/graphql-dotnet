namespace GraphQL.DataLoader
{
    /// <summary>
    /// A data loader that returns a list of values for each given unique key
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="T">The type of the return value</typeparam>
    public class CollectionBatchDataLoader<TKey, T> : DataLoaderBase<TKey, IEnumerable<T>>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;

        /// <summary>
        /// Initializes a new instance of CollectionBatchDataLoader with the specified fetch delegate
        /// </summary>
        /// <param name="fetchDelegate">An asynchronous delegate that is passed a list of keys and cancellation token, which returns an ILookup of keys and values</param>
        /// <param name="keyComparer">An optional equality comparer for the keys</param>
        /// <param name="maxBatchSize">The maximum number of keys passed to the fetch delegate at a time</param>
        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> fetchDelegate,
               IEqualityComparer<TKey>? keyComparer = null,
               int maxBatchSize = int.MaxValue) : base(keyComparer, maxBatchSize)
        {
            _loader = fetchDelegate ?? throw new ArgumentNullException(nameof(fetchDelegate));
        }

        /// <summary>
        /// Initializes a new instance of CollectionBatchDataLoader with the specified fetch delegate and key selector
        /// </summary>
        /// <param name="fetchDelegate">An asynchronous delegate that is passed a list of keys and a cancellation token, which returns a list objects</param>
        /// <param name="keySelector">A selector for the key from the returned object</param>
        /// <param name="keyComparer">An optional equality comparer for the keys</param>
        /// <param name="maxBatchSize">The maximum number of keys passed to the fetch delegate at a time</param>
        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> fetchDelegate,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey>? keyComparer = null,
            int maxBatchSize = int.MaxValue) : base(keyComparer, maxBatchSize)
        {
            if (fetchDelegate == null)
                throw new ArgumentNullException(nameof(fetchDelegate));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            _loader = async (keys, cancellationToken) =>
            {
                var ret = await fetchDelegate(keys, cancellationToken).ConfigureAwait(false);
                return ret.ToLookup(keySelector, keyComparer);
            };
        }

        /// <inheritdoc/>
        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, IEnumerable<T>>> list, CancellationToken cancellationToken)
        {
            var keys = list.Select(x => x.Key);
            var lookup = await _loader(keys, cancellationToken).ConfigureAwait(false);
            foreach (var item in list)
            {
                item.SetResult(lookup[item.Key]);
            }
        }
    }
}
