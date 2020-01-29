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

            async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously load data for the provided given keys
        /// </summary>
        /// <param name="dataLoader">The dataloader to use</param>
        /// <param name="keys">Keys to use for loading data</param>
        /// <returns>
        /// A task that will complete when the DataLoader has been dispatched,
        /// or a completed task if the results are already cached.
        /// </returns>
        public static IDataLoaderResult<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, params TKey[] keys)
        {
            return dataLoader.LoadAsync(keys.AsEnumerable());
        }

        /// <summary>
        /// Chains post-processing to a pending data loader operation
        /// </summary>
        /// <typeparam name="T">The type of the data loader return value</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="parent">The pending data loader operation</param>
        /// <param name="chainedDelegate">The delegate to execute once the data loader finishes loading</param>
        /// <returns>A pending data loader operation that can return a value once the data loader and the chained delegate finish</returns>
        public static IDataLoaderResult<TResult> Then<T, TResult>(this IDataLoaderResult<T> parent, Func<T, CancellationToken, Task<TResult>> chainedDelegate)
        {
            return new DataLoaderChainedResult<T, TResult>(parent, chainedDelegate);
        }

        /// <summary>
        /// Chains post-processing to a pending data loader operation
        /// </summary>
        /// <typeparam name="T">The type of the data loader return value</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="parent">The pending data loader operation</param>
        /// <param name="chainedDelegate">The delegate to execute once the data loader finishes loading</param>
        /// <returns>A pending data loader operation that can return a value once the data loader and the chained delegate finish</returns>
        public static IDataLoaderResult<TResult> Then<T, TResult>(this IDataLoaderResult<T> parent, Func<T, Task<TResult>> chainedDelegate)
        {
            return new DataLoaderChainedResult<T, TResult>(parent, chainedDelegate);
        }

        /// <summary>
        /// Chains post-processing to a pending data loader operation
        /// </summary>
        /// <typeparam name="T">The type of the data loader return value</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="parent">The pending data loader operation</param>
        /// <param name="chainedDelegate">The delegate to execute once the data loader finishes loading</param>
        /// <returns>A pending data loader operation that can return a value once the data loader and the chained delegate finish</returns>
        public static IDataLoaderResult<TResult> Then<T, TResult>(this IDataLoaderResult<T> parent, Func<T, TResult> chainedDelegate)
        {
            return new DataLoaderChainedResult<T, TResult>(parent, (value) => Task.FromResult(chainedDelegate(value)));
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
