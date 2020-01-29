using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// An abstract class for data loaders that accept a key and return a value, with optional caching of keys with values.
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <remarks>This class is thread safe.</remarks>
    public abstract partial class DataLoaderBase<TKey, T>: IDataLoader, IDataLoader<TKey, T>
    {
        //this class supports adding more items after DispatchAsync has been called,
        //  and calling DispatchAsync will then load those items

        private DataLoaderList _list;
        private readonly Dictionary<TKey, DataLoaderPair<TKey, T>> _cachedList;
        private readonly object _sync = new object();
        protected internal readonly IEqualityComparer<TKey> EqualityComparer;

        /// <summary>
        /// Initialize a DataLoaderBase with caching and the default equality comparer
        /// </summary>
        public DataLoaderBase() : this(true, null) { }

        /// <summary>
        /// Initialize a DataLoaderBase the default equality comparer and caching if specified
        /// </summary>
        public DataLoaderBase(bool caching) : this(caching, null) { }

        /// <summary>
        /// Initialize a DataLoaderBase with caching and the specified equality comparer
        /// </summary>
        public DataLoaderBase(IEqualityComparer<TKey> equalityComparer) : this(true, equalityComparer) { }

        /// <summary>
        /// Initialize a DataLoaderBase with the specified equality comparer and caching if specified
        /// </summary>
        public DataLoaderBase(bool caching, IEqualityComparer<TKey> equalityComparer)
        {
            EqualityComparer = equalityComparer ?? EqualityComparer<TKey>.Default;
            if (caching)
                _cachedList = new Dictionary<TKey, DataLoaderPair<TKey, T>>(equalityComparer);
        }

        /// <summary>
        /// Asynchronously load data for the provided given key
        /// </summary>
        /// <param name="key">Key to use for loading data</param>
        /// <returns>
        /// An object representing a pending operation
        /// </returns>
        public virtual IDataLoaderResult<T> LoadAsync(TKey key)
        {
            lock (_sync)
            {
                //once it enters the lock, it is guaranteed to exit the lock, as it does not depend on external code
                if (_cachedList != null)
                {
                    if (_cachedList.TryGetValue(key, out var ret2))
                        return ret2;
                }
                if (_list != null)
                {
                    if (_list.TryGetValue(key, out var ret2))
                        return ret2;
                }
                else
                {
                    _list = new DataLoaderList(this);
                }
                var ret = new DataLoaderPair<TKey, T>(_list, key);
                _list.Add(key, ret);
                _cachedList?.Add(key, ret);
                return ret;
            }
        }

        /// <summary>
        /// Returns an IDataLoaderResult that will return the specified value
        /// </summary>
        public static IDataLoaderResult<T> FromResult(T result) => new DataLoaderResult<T>(result);

        /// <summary>
        /// Returns an IDataLoaderResult that will return the specified asynchronous task
        /// </summary>
        public static IDataLoaderResult<T> FromResult(Task<T> result) => new DataLoaderResult<T>(result);

        /// <summary>
        /// An abstract asynchronous function to load the values for a given list of keys
        /// </summary>
        /// <remarks>
        /// This may be called on multiple threads if IDataLoader.LoadAsync is called on multiple threads.
        /// It will never be called for the same list of items.
        /// </remarks>
        protected abstract Task FetchAsync(IEnumerable<DataLoaderPair<TKey, T>> list, CancellationToken cancellationToken);

        /// <summary>
        /// Internally used by DataLoaderList to start the fetch operation
        /// </summary>
        /// <returns>A Task representing the asynchronous fetch operation</returns>
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

        /// <summary>
        /// Dispatch any pending operations
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to the fetch delegate</param>
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
