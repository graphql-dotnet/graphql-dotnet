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

            var cts = Interlocked.Exchange(ref _completionSource, new TaskCompletionSource<T>());

            if (cancellationToken.IsCancellationRequested)
            {
                // If cancellation has been requested already,
                // set the task to canceled without calling FetchAsync()
                cts.TrySetCanceled();
                return;
            }

            FetchAsync(cancellationToken).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Exception ex = task.Exception;

                    while (ex is AggregateException && ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }

                    cts.SetException(ex);
                }
                else if (task.IsCanceled)
                {
                    cts.SetCanceled();
                }
                else
                {
                    cts.SetResult(task.Result);
                }
            });
        }
    }
}
