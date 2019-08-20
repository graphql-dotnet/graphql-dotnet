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
                if (!_completionSource.Task.IsCompleted && !_dispatchNeededSource.Task.IsCompleted)
                {
                    // DataLoaded is used by the LoadAsync method of the DataLoader, and if not
                    // completed it means the dataloader needs dispatch, so complete the
                    // DispatchNeeded task thru the TaskCompletionSource
                    _dispatchNeededSource.SetResult(true);
                }
                return _completionSource.Task;
            }
        }
        private TaskCompletionSource<bool> _dispatchNeededSource = CreateAwaitedCompletionsSource();

        private TaskCompletionSource<T> _completionSource = CreateCompletionsSource();
        private object _tcsLock = new object();

        private static TaskCompletionSource<T> CreateCompletionsSource() => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static TaskCompletionSource<bool> CreateAwaitedCompletionsSource() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        protected abstract bool IsFetchNeeded();

        /// <summary>
        /// A task that is completed when this loader is in need of dispatch (I.e. a LoadAsync call is requesting data that is not cached/ready)
        /// </summary>
        public Task DispatchNeeded => _dispatchNeededSource.Task;

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
                // Here the underlying tcs of DataLoaded is recreated, so recreate the tcs for DispatchNeeded aswell
                _dispatchNeededSource = CreateAwaitedCompletionsSource();
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
