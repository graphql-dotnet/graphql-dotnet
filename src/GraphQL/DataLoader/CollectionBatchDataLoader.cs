using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class CollectionBatchDataLoader<TKey, T> : IDataLoader<TKey, IEnumerable<T>>, IDispatchableDataLoader
    {
        private readonly Action<IDispatchableDataLoader> _queueFunc;
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> _loader;
        private readonly object _lock = new object();
        private readonly Dictionary<TKey, TaskCompletionSource<IEnumerable<T>>> _taskCompletionSources;
        private readonly HashSet<TKey> _pendingKeys;

        internal CollectionBatchDataLoader(Action<IDispatchableDataLoader> queueFunc,
            Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null)
        {
            _queueFunc = queueFunc ?? throw new ArgumentNullException(nameof(queueFunc));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _taskCompletionSources = new Dictionary<TKey, TaskCompletionSource<IEnumerable<T>>>(keyComparer);
        }

        internal CollectionBatchDataLoader(Action<IDispatchableDataLoader> queueFunc,
            Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
        {
            _queueFunc = queueFunc ?? throw new ArgumentNullException(nameof(queueFunc));
            loader = loader ?? throw new ArgumentNullException(nameof(loader));
            keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<ILookup<TKey, T>> LoadAndMapToLookup(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                return values.ToLookup(keySelector, keyComparer);
            }

            _loader = LoadAndMapToLookup;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _taskCompletionSources = new Dictionary<TKey, TaskCompletionSource<IEnumerable<T>>>(keyComparer);
        }

        public Task<IEnumerable<T>> LoadAsync(TKey key)
        {
            lock (_lock)
            {
                if (_taskCompletionSources.TryGetValue(key, out TaskCompletionSource<IEnumerable<T>> taskCompletionSource))
                    return taskCompletionSource.Task;

                taskCompletionSource = new TaskCompletionSource<IEnumerable<T>>();
                _taskCompletionSources.Add(key, taskCompletionSource);
                _pendingKeys.Add(key);
                _queueFunc(this);

                return taskCompletionSource.Task;
            }
        }

        async Task<Task> IDispatchableDataLoader.DispatchAsync(CancellationToken cancellationToken)
        {
            TKey[] keys;

            lock (_lock)
            {
                // Get pending keys and clear pending list
                keys = _pendingKeys.ToArray();
                _pendingKeys.Clear();
            }

            if (!keys.Any())
                return Task.FromResult(0);

            if (cancellationToken.IsCancellationRequested)
            {
                foreach (var key in keys)
                    _taskCompletionSources[key].TrySetCanceled();

                return Task.FromResult(0);
            }

            try
            {
                var result = await _loader(keys, cancellationToken).ConfigureAwait(false);
                return Task.Run(() =>
                {
                    foreach (var key in keys)
                    {
                        var value = result.Contains(key)
                            ? result[key]
                            : Enumerable.Empty<T>();
                        _taskCompletionSources[key].SetResult(value);
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                foreach (var key in keys)
                    _taskCompletionSources[key].TrySetCanceled();
            }
            catch (Exception ex)
            {
                foreach (var key in keys)
                    _taskCompletionSources[key].TrySetException(ex);
            }

            return Task.FromResult(0);
        }
    }
}
