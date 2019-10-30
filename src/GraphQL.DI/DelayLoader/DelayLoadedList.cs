using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    internal class DelayLoadedList<TIn, TOut> : Dictionary<TIn, DelayLoadedPair<TIn, TOut>>, IDelayLoader
    {
        protected readonly DelayLoader<TIn, TOut> _delayLoader;
        private Task LoadingTask;

        public DelayLoadedList(DelayLoader<TIn, TOut> delayLoader) : base(delayLoader.EqualityComparer)
        {
            _delayLoader = delayLoader;
        }

        public Task LoadAsync() => LoadAsync(CancellationToken.None);

        public Task LoadAsync(CancellationToken cancellationToken)
        {
            if (LoadingTask != null) return LoadingTask;
            lock (this) //external code cannot access "this"
            {
                //this lock depends on external code, as it should to prevent double execution
                //it also depends on the locks in DelayLoader to succeed, which is guaranteed to succeed, as if they
                //  are in a lock, they are guaranteed to finish quickly without a dependency on this or external code
                //if two threads call LoadAsync simultaneously, one will block until the Task is created
                //  and then the created Task will be returned to the caller
                //this will pass the cancellationToken from the first caller to LoadAsync through to
                //  StartLoading; further calls to LoadAsync cannot cancel the loading

                //return LoadingTask if already set,
                //  or else start data loading, save the returned Task in LoadingTask, and return the Task
                return LoadingTask ?? (LoadingTask = _delayLoader.StartLoading(this, cancellationToken));
            }
        }
    }

}
