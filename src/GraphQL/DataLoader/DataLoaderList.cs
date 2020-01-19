using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public partial class DataLoaderBase<TKey, T>
    {
        private class DataLoaderList : Dictionary<TKey, DataLoaderPair<TKey, T>>, IDataLoader
        {
            protected readonly DataLoaderBase<TKey, T> _dataLoader;
            private Task LoadingTask;

            public DataLoaderList(DataLoaderBase<TKey, T> delayLoader) : base(delayLoader.EqualityComparer)
            {
                _dataLoader = delayLoader;
            }

            public Task DispatchAsync(CancellationToken cancellationToken = default)
            {
                if (LoadingTask != null)
                    return LoadingTask;
                lock (this) //external code cannot access "this"
                {
                    //this lock depends on external code, as it should to prevent double execution
                    //it also depends on the locks in DataLoader to succeed, which is guaranteed to succeed, as if they
                    //  are in a lock, they are guaranteed to finish quickly without a dependency on this or external code
                    //if two threads call DispatchAsync simultaneously, one will block until the Task is created
                    //  and then the created Task will be returned to both callers
                    //this will pass the cancellationToken from the first caller to DispatchAsync through to
                    //  StartLoading; further calls to DispatchAsync cannot cancel the loading

                    //return LoadingTask if already set,
                    //  or else start data loading, save the returned Task in LoadingTask, and return the Task
                    return LoadingTask ?? (LoadingTask = _dataLoader.StartLoading(this, cancellationToken));
                }
            }
        }
    }
}
