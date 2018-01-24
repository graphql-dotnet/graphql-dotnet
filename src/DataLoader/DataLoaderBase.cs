using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    public abstract class DataLoaderBase<T> : IDataLoader
    {
        protected abstract Task<T> FetchAsync(CancellationToken cancellationToken);

        protected Task<T> DataLoaded => _completionSource.Task;
        private TaskCompletionSource<T> _completionSource = new TaskCompletionSource<T>();

        public void Dispatch(CancellationToken cancellationToken)
        {
            var cts = Interlocked.Exchange(ref _completionSource, new TaskCompletionSource<T>());

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
