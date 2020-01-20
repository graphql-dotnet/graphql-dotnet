using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.DataLoader
{
    public static class DataLoaderExtensions
    {
        /// <summary>
        /// Asynchronously load data for the provided given keys
        /// </summary>
        /// <param name="dataLoader">The dataloader to use</param>
        /// <param name="keys">Keys to use for loading data</param>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        public static IDataLoaderResult<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, IEnumerable<TKey> keys)
        {
            var results = new List<IDataLoaderResult<T>>();

            foreach (var key in keys)
            {
                results.Add(dataLoader.LoadAsync(key));
            }

            return new DataLoaderResultWhenAll<T>(results);
        }

        private class DataLoaderResultWhenAll<T> : IDataLoaderResult<T[]>
        {
            private readonly IEnumerable<IDataLoaderResult<T>> _dataLoaderResults;

            public DataLoaderResultWhenAll(IEnumerable<IDataLoaderResult<T>> dataLoaderResults)
            {
                _dataLoaderResults = dataLoaderResults ?? throw new ArgumentNullException(nameof(dataLoaderResults));
            }

            public Task<T[]> GetResultAsync(CancellationToken cancellationToken = default)
            {
                return Task.WhenAll(_dataLoaderResults.Select(x => x.GetResultAsync(cancellationToken)));
            }

            async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously load data for the provided given keys
        /// </summary>
        /// <param name="dataLoader">The dataloader to use</param>
        /// <param name="keys">Keys to use for loading data</param>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the result is already cached.
        /// </returns>
        public static IDataLoaderResult<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, params TKey[] keys)
        {
            return dataLoader.LoadAsync(keys.AsEnumerable());
        }

        public static IDataLoaderResult<TResult> Then<TOut, TResult>(this IDataLoaderResult<TOut> parent, Func<TOut, Task<TResult>> func)
        {
            return new ContinueWith<TOut, TResult>(parent, func);
        }

        public static IDataLoaderResult<TResult> Then<TOut, TResult>(this IDataLoaderResult<TOut> parent, Func<TOut, TResult> func)
        {
            return new ContinueWith<TOut, TResult>(parent, (value) => Task.FromResult(func(value)));
        }

        public static IDataLoaderResult<TResult> Then<TOut, TResult>(this IDataLoaderResult<TOut> parent, Func<TOut, CancellationToken, Task<TResult>> func)
        {
            return new ContinueWith<TOut, TResult>(parent, func);
        }

        public static IDataLoaderResult<TResult> Then<TOut, TResult>(this IDataLoaderResult<TOut> parent, Func<TOut, CancellationToken, TResult> func)
        {
            return new ContinueWith<TOut, TResult>(parent, (value, cancellationToken) => Task.FromResult(func(value, cancellationToken)));
        }

        public static FieldBuilder<TSourceType, TReturnType> ResolveAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<TReturnType>> resolve)
        {
            return builder.Resolve(new FuncFieldResolver<TSourceType, IDataLoaderResult<TReturnType>>(resolve));
        }

        public static FieldBuilder<TSourceType, TReturnType> ResolveAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<IDataLoaderResult<TReturnType>>> resolve)
        {
            return builder.Resolve(new AsyncFieldResolver<TSourceType, IDataLoaderResult<TReturnType>>(resolve));
        }
    }
}
