namespace GraphQL.DataLoader
{
    //this class could always be unsealed, but it seems pointless, as
    //  the DataLoaderBase class always creates the DataLoaderPair instances,
    //  and there's really no other code that belongs within this class

    /// <summary>
    /// An implementation of an IDataLoaderResult that calls IDataLoader.DispatchAsync when its result is requested
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="T">The type of the return value</typeparam>
    public sealed class DataLoaderPair<TKey, T> : IDataLoaderResult<T>
    {
        /// <summary>
        /// Initialize an instance of DataLoaderPair with the specified data loader and key
        /// </summary>
        public DataLoaderPair(IDataLoader loader, TKey key)
        {
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            Key = key;
        }

        private T _result = default!;

        /// <summary>
        /// Returns the key that is passed to the data loader's fetch delegate
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Returns the data loader that is called when the result is requested
        /// </summary>
        public IDataLoader Loader { get; }

        /// <summary>
        /// Returns the result if it has been set, or throws an exception if not
        /// </summary>
        public T Result => IsResultSet ? _result : throw new InvalidOperationException("Result has not been set");

        /// <summary>
        /// Returns a boolean that indicates if the result has been set
        /// </summary>
        public bool IsResultSet { get; private set; }

        /// <summary>
        /// Sets the result if it has not yet been set
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws when the result has already been set</exception>
        public void SetResult(T value)
        {
            if (IsResultSet)
                throw new InvalidOperationException("Result has already been set");
            _result = value;
            IsResultSet = true;
        }

        /// <summary>
        /// Asynchronously executes the loader if it has not yet been executed; then returns the result
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        public async Task<T> GetResultAsync(CancellationToken cancellationToken = default)
        {
            if (!IsResultSet)
                await Loader.DispatchAsync(cancellationToken).ConfigureAwait(false);
            return Result;
        }

        async Task<object?> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken)
        {
            if (!IsResultSet)
                await Loader.DispatchAsync(cancellationToken).ConfigureAwait(false);
            return Result;
        }
    }
}
