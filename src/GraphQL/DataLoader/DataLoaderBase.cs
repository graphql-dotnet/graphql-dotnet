using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public abstract class DataLoaderBase<T> : IDataLoader
    {
        protected abstract Task<T> FetchAsync(CancellationToken cancellationToken);

        protected Task<T> DataLoaded => _completionSource.Task;
        private TaskCompletionSource<T> _completionSource = new TaskCompletionSource<T>();

        protected abstract bool IsFetchNeeded();

        public void Dispatch(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsFetchNeeded())
            {
                return;
            }

            var tcs = Interlocked.Exchange(ref _completionSource, new TaskCompletionSource<T>());

            if (cancellationToken.IsCancellationRequested)
            {
                // If cancellation has been requested already,
                // set the task to cancelled without calling FetchAsync()
                tcs.TrySetCanceled();
                return;
            }

            Task<T> fetchTask = null;

            try
            {
                fetchTask = FetchAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            fetchTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Exception ex = task.Exception;

                    while (ex is AggregateException && ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }

                    tcs.SetException(ex);
                }
                else if (task.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(task.Result);
                }
            });
        }
    }
}
