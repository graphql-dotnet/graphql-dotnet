using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class CollectionBatchDataLoader<TKey, T> : DataLoaderBase<TKey, IEnumerable<T>>, IDataLoader<TKey, IEnumerable<T>>
    {
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;
        private readonly T _defaultValue;

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> loader,
               IEqualityComparer<TKey> keyComparer = null,
               T defaultValue = default) : base(keyComparer)
        {
            _loader = loader;
            _defaultValue = defaultValue;
        }

        public CollectionBatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default) : base(keyComparer)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            _loader = async (keys, cancellationToken) =>
            {
                var ret = await loader(keys, cancellationToken);
                return ret.ToLookup(keySelector);
            };
            _defaultValue = defaultValue;
        }

        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, IEnumerable<T>>> list, CancellationToken cancellationToken)
        {
            var keys = list.Select(x => x.Key);
            var lookup = await _loader(keys, cancellationToken);
            foreach (var item in list)
            {
                item.SetResult(lookup[item.Key]);
            }
        }
    }
}
