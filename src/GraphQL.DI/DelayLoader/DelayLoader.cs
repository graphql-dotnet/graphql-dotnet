using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public abstract class DelayLoader<TIn, TOut> : IDelayLoader
    {
        //this class is designed to support multithreaded operation
        //it also supports adding more items after LoadAsync has been called,
        //  and calling LoadAsync will then load those items

        private DelayLoadedList<TIn, TOut> list;
        private Dictionary<TIn, DelayLoadedPair<TIn, TOut>> cachedList;
        private object sync = new object();
        protected internal readonly IEqualityComparer<TIn> EqualityComparer;

        public DelayLoader() : this(false, null) { }
        public DelayLoader(bool caching) : this(caching, null) { }
        public DelayLoader(IEqualityComparer<TIn> equalityComparer) : this(false, equalityComparer) { }

        public DelayLoader(bool caching, IEqualityComparer<TIn> equalityComparer)
        {
            EqualityComparer = equalityComparer ?? EqualityComparer<TIn>.Default;
            if (caching) cachedList = new Dictionary<TIn, DelayLoadedPair<TIn, TOut>>();
        }

        public virtual IDelayLoadedResult<TOut> Queue(TIn inputValue)
        {
            lock (sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                if (list == null) list = new DelayLoadedList<TIn, TOut>(this);
                if (cachedList != null)
                {
                    if (cachedList.TryGetValue(inputValue, out var ret2)) return ret2;
                }
                else
                {
                    if (list.TryGetValue(inputValue, out var ret2)) return ret2;
                }
                var ret = new DelayLoadedPair<TIn, TOut>(list, inputValue);
                list.Add(inputValue, ret);
                cachedList?.Add(inputValue, ret);
                return ret;
            }
        }

        public static IDelayLoadedResult<TOut> FromResult(TOut outputValue)
        {
            return new PredefinedResponse<TOut>(outputValue);
        }

        //this function may be called on multiple threads only if IDelayLoader.LoadAsync
        //  is called on multiple threads for different lists of queued items
        public abstract Task LoadValuesAsync(IEnumerable<DelayLoadedPair<TIn, TOut>> list, CancellationToken cancellationToken);

        internal Task StartLoading(DelayLoadedList<TIn, TOut> listToLoad, CancellationToken cancellationToken)
        {
            if (listToLoad == null) throw new ArgumentNullException(nameof(listToLoad));
            lock (sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                if (list == listToLoad) list = null;
            }
            return this.LoadValuesAsync(listToLoad.Values, cancellationToken);
        }

        public Task LoadAsync() => LoadAsync(CancellationToken.None);

        public Task LoadAsync(CancellationToken cancellationToken)
        {
            //start loading the currently queued items
            DelayLoadedList<TIn, TOut> listToLoad;
            lock (sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                listToLoad = list;
                list = null;
            }
            if (listToLoad == null) return Task.CompletedTask;
            return listToLoad.LoadAsync(cancellationToken);
        }
    }

}
