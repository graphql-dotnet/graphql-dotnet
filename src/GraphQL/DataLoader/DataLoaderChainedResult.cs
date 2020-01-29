using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    /// <summary>
    /// Allows for post processing after a data loader has executed
    /// </summary>
    /// <typeparam name="T">The type of the data loader's result</typeparam>
    /// <typeparam name="TResult">The type of the returned value</typeparam>
    public sealed class DataLoaderChainedResult<T, TResult> : IDataLoaderResult<TResult>
    {
        private readonly IDataLoaderResult<T> _parent;
        private readonly Func<T, CancellationToken, Task<TResult>> _chainedDelegate;

        /// <summary>
        /// Initializes an instance of DataLoaderChainedResult with the given parent data loader and chained asynchronous delegate
        /// </summary>
        /// <param name="parent">The pending data loader operation</param>
        /// <param name="chainedDelegate">A delegate with the data loader's return value as a parameter, returning an asynchronous task</param>
        public DataLoaderChainedResult(IDataLoaderResult<T> parent, Func<T, Task<TResult>> chainedDelegate)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            if (chainedDelegate == null) throw new ArgumentNullException(nameof(chainedDelegate));
            _chainedDelegate = (value, token) => chainedDelegate(value);
        }

        /// <summary>
        /// Initializes an instance of DataLoaderChainedResult with the given parent data loader and chained asynchronous delegate
        /// </summary>
        /// <param name="parent">The pending data loader operation</param>
        /// <param name="chainedDelegate">A delegate with the data loader's return value and a cancellation token as parameters, returning an asynchronous task</param>
        public DataLoaderChainedResult(IDataLoaderResult<T> parent, Func<T, CancellationToken, Task<TResult>> chainedDelegate)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _chainedDelegate = chainedDelegate ?? throw new ArgumentNullException(nameof(chainedDelegate));
        }

        /// <summary>
        /// Asynchronously executes the loader if it has not yet been executed; then returns the result
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        /// <remarks>If called a second time, the chainedDelegate will run again also; the final result is not cached</remarks>
        public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
        {
            var result = await _parent.GetResultAsync(cancellationToken).ConfigureAwait(false);
            return await _chainedDelegate(result, cancellationToken).ConfigureAwait(false);
        }

        async Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken).ConfigureAwait(false);
    }
}
