using System;
using System.Collections.Generic;
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

        public async Task DispatchAsync(CancellationToken cancellationToken = default)
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
        protected static IEnumerable<IEnumerable<TKey>> Batch<TKey>(IEnumerable<TKey> collection, int batchSize)
        {
            var nextbatch = new List<TKey>(batchSize);
            foreach (var item in collection)
            {
                nextbatch.Add(item);
                if (nextbatch.Count == batchSize)
                {
                    yield return nextbatch;
                    nextbatch = new List<TKey>(batchSize);
                }
            }
            if (nextbatch.Count > 0)
                yield return nextbatch;
        }
    }
}
