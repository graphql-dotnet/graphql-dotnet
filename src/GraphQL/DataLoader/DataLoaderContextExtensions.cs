using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public static class DataLoaderContextExtensions
    {
        public static Func<CancellationToken, TResult> WrapNonCancellableFunc<TResult>(Func<TResult> func) => (cancellationToken) => func();

        public static Func<T, CancellationToken, TResult> WrapNonCancellableFunc<T, TResult>(Func<T, TResult> func) => (arg, cancellationToken) => func(arg);

        /// <summary>
        /// Get or add a DataLoader instance for caching data fetching operations.
        /// </summary>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A cancellable delegate to fetch data asynchronously</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<T> GetOrAddLoader<T>(this DataLoaderContext context, string loaderKey, Func<CancellationToken, Task<T>> fetchFunc)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new SimpleDataLoader<T>(fetchFunc));
        }

        /// <summary>
        /// Get or add a DataLoader instance for caching data fetching operations.
        /// </summary>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A delegate to fetch data asynchronously</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<T> GetOrAddLoader<T>(this DataLoaderContext context, string loaderKey, Func<Task<T>> fetchFunc)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new SimpleDataLoader<T>(WrapNonCancellableFunc(fetchFunc)));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A cancellable delegate to fetch data for some keys asynchronously</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<Dictionary<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null, T defaultValue = default(T))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(fetchFunc, keyComparer, defaultValue));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A delegate to fetch data for some keys asynchronously</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<Dictionary<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null, T defaultValue = default(T))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keyComparer, defaultValue));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc"></param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null, T defaultValue = default(T))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(fetchFunc, keySelector, keyComparer, defaultValue));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A delegate to fetch data for some keys asynchronously</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null, T defaultValue = default(T))
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keySelector, keyComparer, defaultValue));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A cancellable delegate to fetch data for some keys asynchronously</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(fetchFunc, keyComparer));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A delegate to fetch data for some keys asynchronously</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<ILookup<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keyComparer));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A cancellable delegate to fetch data for some keys asynchronously</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(fetchFunc, keySelector, keyComparer));
        }

        /// <summary>
        /// Get or add a DataLoader instance for batching data fetching operations.
        /// </summary>
        /// <typeparam name="TKey">The type of key used to load data</typeparam>
        /// <typeparam name="T">The type of data to be loaded</typeparam>
        /// <param name="context">The <seealso cref="DataLoaderContext"/> to get or add a DataLoader to</param>
        /// <param name="loaderKey">A unique key to identify the DataLoader instance</param>
        /// <param name="fetchFunc">A delegate to fetch data for some keys asynchronously</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="keyComparer">An <seealso cref="IEqualityComparer<T>"/> to compare keys.</param>
        /// <returns>A new or existing DataLoader instance</returns>
        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keySelector, keyComparer));
        }
    }
}
