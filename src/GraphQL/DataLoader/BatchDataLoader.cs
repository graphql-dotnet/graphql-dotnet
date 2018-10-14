using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class BatchDataLoader<TKey, T> : IDataLoader<TKey, T>, IDispatchableDataLoader
    {
        private readonly Action<IDispatchableDataLoader> _queueFunc;
        private readonly Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> _loader;
        private readonly object _lock = new object();
        private readonly Dictionary<TKey, TaskCompletionSource<T>> _taskCompletionSources;
        private readonly HashSet<TKey> _pendingKeys;
        private readonly T _defaultValue;

        internal BatchDataLoader(Action<IDispatchableDataLoader> queueFunc,
            Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> loader,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T))
        {
            _queueFunc = queueFunc ?? throw new ArgumentNullException(nameof(queueFunc));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _taskCompletionSources = new Dictionary<TKey, TaskCompletionSource<T>>(keyComparer);
            _defaultValue = defaultValue;
        }

        internal BatchDataLoader(Action<IDispatchableDataLoader> queueFunc,
            Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> loader,
            Func<T, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null,
            T defaultValue = default(T))
        {
            _queueFunc = queueFunc ?? throw new ArgumentNullException(nameof(queueFunc));
            loader = loader ?? throw new ArgumentNullException(nameof(loader));
            keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            async Task<IDictionary<TKey, T>> LoadAndMapToDictionary(IEnumerable<TKey> keys, CancellationToken cancellationToken)
            {
                var values = await loader(keys, cancellationToken).ConfigureAwait(false);
                return values.ToDictionary(keySelector, keyComparer);
            }

            _loader = LoadAndMapToDictionary;
            _pendingKeys = new HashSet<TKey>(keyComparer);
            _taskCompletionSources = new Dictionary<TKey, TaskCompletionSource<T>>(keyComparer);
            _defaultValue = defaultValue;
        }

        public Task<T> LoadAsync(TKey key)
        {
            lock (_lock)
            {
                if (_taskCompletionSources.TryGetValue(key, out TaskCompletionSource<T> taskCompletionSource))
                    return taskCompletionSource.Task;

                taskCompletionSource = new TaskCompletionSource<T>();
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
                        if (result == null || !result.TryGetValue(key, out T value))
                            value = _defaultValue;
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
