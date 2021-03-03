using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.DataLoader
{
    public class RelayBatchDataLoader<TKey, T> : DataLoaderBase<IDictionary<TKey, Connection<T>>>, IRelayDataLoader<TKey, T>
    {
        private readonly Func<IEnumerable<PageRequest<TKey>>, CancellationToken, Task<IDictionary<TKey, Connection<T>>>> _loader;
        private readonly HashSet<PageRequest<TKey>> _pendingRequests;

        public RelayBatchDataLoader(Func<IEnumerable<PageRequest<TKey>>, CancellationToken, Task<IDictionary<TKey, Connection<T>>>> loader, IEqualityComparer<PageRequest<TKey>> keyComparer = null)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            keyComparer ??= EqualityComparer<PageRequest<TKey>>.Default;
            _pendingRequests = new HashSet<PageRequest<TKey>>(keyComparer);
        }

        protected override bool IsFetchNeeded()
        {
            lock (_pendingRequests)
            {
                return _pendingRequests.Count > 0;
            }
        }

        protected override async Task<IDictionary<TKey, Connection<T>>> FetchAsync(CancellationToken cancellationToken)
        {
            IList<PageRequest<TKey>> pageRequests;

            lock (_pendingRequests)
            {
                pageRequests = _pendingRequests.ToArray();
                _pendingRequests.Clear();
            }

            var responses = await _loader(pageRequests, cancellationToken);

            return responses;
        }

        public async Task<Connection<T>> LoadPageAsync(PageRequest<TKey> pageRequest)
        {
            lock (_pendingRequests)
            {
                _pendingRequests.Add(pageRequest);
            }

            var result = await DataLoaded.ConfigureAwait(false);
            return result.TryGetValue(pageRequest.Key, out var value) ? value : default;
        }
    }
}
