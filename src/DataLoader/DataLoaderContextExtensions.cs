using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    public static class DataLoaderContextExtensions
    {
        public static Func<CancellationToken, TResult> WrapNonCancellableFunc<TResult>(Func<TResult> func) => (cancellationToken) => func();

        public static Func<T, CancellationToken, TResult> WrapNonCancellableFunc<T, TResult>(Func<T, TResult> func) => (arg, cancellationToken) => func(arg);

        public static IDataLoader<T> GetOrAddLoader<T>(this DataLoaderContext context, string loaderKey, Func<CancellationToken, Task<T>> fetchFunc)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new SimpleDataLoader<T>(fetchFunc));
        }

        public static IDataLoader<T> GetOrAddLoader<T>(this DataLoaderContext context, string loaderKey, Func<Task<T>> fetchFunc)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new SimpleDataLoader<T>(WrapNonCancellableFunc(fetchFunc)));
        }

        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<Dictionary<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(fetchFunc, keyComparer));
        }

        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<Dictionary<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keyComparer));
        }

        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(fetchFunc, keySelector, keyComparer));
        }

        public static IDataLoader<TKey, T> GetOrAddBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<IEnumerable<T>>> fetchFunc,
            Func<T, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return context.GetOrAdd(loaderKey, () => new BatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keySelector, keyComparer));
        }

        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(fetchFunc, keyComparer));
        }

        public static IDataLoader<TKey, IEnumerable<T>> GetOrAddCollectionBatchLoader<TKey, T>(this DataLoaderContext context, string loaderKey, Func<IEnumerable<TKey>, Task<ILookup<TKey, T>>> fetchFunc,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (fetchFunc == null)
                throw new ArgumentNullException(nameof(fetchFunc));

            return context.GetOrAdd(loaderKey, () => new CollectionBatchDataLoader<TKey, T>(WrapNonCancellableFunc(fetchFunc), keyComparer));
        }

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
