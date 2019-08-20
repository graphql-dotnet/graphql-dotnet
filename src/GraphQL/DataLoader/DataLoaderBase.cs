using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public abstract class DataLoaderBase<T> : IDataLoader
    {
        protected abstract Task<T> FetchAsync(CancellationToken cancellationToken);


        protected Task<T> DataLoaded
        {
            get
            {
                if (!_completionSource.Task.IsCompleted && !_awaitedSource.Task.IsCompleted)
                {
                    // Someone is awaiting this dataloader and data is not loaded,
                    // so complete the LoaderAwaited task
                    _awaitedSource.SetResult(true);
                }
                return _completionSource.Task;
            }
        }
        private TaskCompletionSource<bool> _awaitedSource = CreateAwaitedCompletionsSource();

        private TaskCompletionSource<T> _completionSource = CreateCompletionsSource();
        private object _tcsLock = new object();

        private static TaskCompletionSource<T> CreateCompletionsSource() => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static TaskCompletionSource<bool> CreateAwaitedCompletionsSource() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        protected abstract bool IsFetchNeeded();

        /// <summary>
        /// A task that is completed when this loader is awaited and in need of dispatch
        /// </summary>
        public Task LoaderAwaited => _awaitedSource.Task;

        public async Task DispatchAsync(CancellationToken cancellationToken = default)
        {
            if (!IsFetchNeeded())
            {
                return;
            }

            var tcs = (TaskCompletionSource<T>)null;
            lock (_tcsLock)
            {
                tcs = _completionSource;
                _completionSource = CreateCompletionsSource();
                // Here the underlying tcs of DataLoaded is recreated, so recreate the tcs for LoaderAwaited aswell
                _awaitedSource = CreateAwaitedCompletionsSource();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // If cancellation has been requested already,
                // set the task to cancelled without calling FetchAsync()
                tcs.TrySetCanceled();
                return;
            }

            try
            {
                var result = await FetchAsync(cancellationToken)
                    .ConfigureAwait(false);

                tcs.SetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }
}
