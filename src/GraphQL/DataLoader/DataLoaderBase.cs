using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public abstract partial class DataLoaderBase<TKey, T>: IDataLoader
    {
        //this class is designed to support multithreaded operation
        //it also supports adding more items after LoadAsync has been called,
        //  and calling LoadAsync will then load those items

        private DataLoaderList _list;
        private readonly Dictionary<TKey, DataLoaderPair<TKey, T>> _cachedList;
        private readonly object _sync = new object();
        protected internal readonly IEqualityComparer<TKey> EqualityComparer;

        public DataLoaderBase() : this(true, null) { }
        public DataLoaderBase(bool caching) : this(caching, null) { }
        public DataLoaderBase(IEqualityComparer<TKey> equalityComparer) : this(true, equalityComparer) { }

        public DataLoaderBase(bool caching, IEqualityComparer<TKey> equalityComparer)
        {
            EqualityComparer = equalityComparer ?? EqualityComparer<TKey>.Default;
            if (caching)
                _cachedList = new Dictionary<TKey, DataLoaderPair<TKey, T>>();
        }

        public virtual IDataLoaderResult<T> LoadAsync(TKey inputValue)
        {
            lock (_sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                if (_cachedList != null)
                {
                    if (_cachedList.TryGetValue(inputValue, out var ret2))
                        return ret2;
                }
                if (_list != null)
                {
                    if (_list.TryGetValue(inputValue, out var ret2))
                        return ret2;
                }
                else
                {
                    _list = new DataLoaderList(this);
                }
                var ret = new DataLoaderPair<TKey, T>(_list, inputValue);
                _list.Add(inputValue, ret);
                _cachedList?.Add(inputValue, ret);
                return ret;
            }
        }

        public static IDataLoaderResult<T> FromResult(T outputValue) => new DataLoaderResult<T>(Task.FromResult(outputValue));

        public static IDataLoaderResult<T> FromResult(Task<T> outputTask) => new DataLoaderResult<T>(outputTask);

        //this function may be called on multiple threads only if IDataLoader.LoadAsync
        //  is called on multiple threads for different lists of queued items
        //it will never be called on multiple threads for the same list of items
        protected abstract Task FetchAsync(IEnumerable<DataLoaderPair<TKey, T>> list, CancellationToken cancellationToken);

        private Task StartLoading(DataLoaderList listToLoad, CancellationToken cancellationToken)
        {
            if (listToLoad == null)
                throw new ArgumentNullException(nameof(listToLoad));
            lock (_sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                if (_list == listToLoad)
                    _list = null;
            }
            return FetchAsync(listToLoad.Values, cancellationToken);
        }

        public Task DispatchAsync(CancellationToken cancellationToken = default)
        {
            //start loading the currently queued items
            DataLoaderList listToLoad;
            lock (_sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                //cannot use Interlocked.Exchange here because that can execute during another lock
                listToLoad = _list;
                _list = null;
            }
            if (listToLoad == null)
                return Task.CompletedTask;
            return listToLoad.DispatchAsync(cancellationToken);
        }
    }
}
