using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public class SimpleDataLoader<T> : IDataLoader<T>, IDispatchableDataLoader
    {
        private readonly Action<IDispatchableDataLoader> _queueFunc;
        private readonly Func<CancellationToken, Task<T>> _loader;
        private readonly object _lock = new object();
        private TaskCompletionSource<T> _taskCompletionSource = null;

        internal SimpleDataLoader(Action<IDispatchableDataLoader> queueFunc, Func<CancellationToken, Task<T>> loader)
        {
            _queueFunc = queueFunc ?? throw new ArgumentNullException(nameof(queueFunc));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public Task<T> LoadAsync()
        {
            if (_taskCompletionSource != null)
                return _taskCompletionSource.Task;

            lock (_lock)
            {
                if (_taskCompletionSource == null)
                {
                    _taskCompletionSource = new TaskCompletionSource<T>();
                    _queueFunc(this);
                }

                return _taskCompletionSource.Task;
            }
        }

        async Task<Task> IDispatchableDataLoader.DispatchAsync(CancellationToken cancellationToken)
        {
            if (_taskCompletionSource == null)
                return Task.FromResult(0);

            if (cancellationToken.IsCancellationRequested)
            {
                _taskCompletionSource.TrySetCanceled();
                return Task.FromResult(0);
            }

            try
            {
                var result = await _loader(cancellationToken).ConfigureAwait(false);
                return Task.Run(() => _taskCompletionSource.SetResult(result), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _taskCompletionSource.TrySetCanceled();
            }
            catch (Exception ex)
            {
                _taskCompletionSource.TrySetException(ex);
            }

            return Task.FromResult(0);
        }
    }
}
