namespace GraphQL.DataLoader
{
    /// <summary>
    /// Represents a pending operation that can return a value
    /// </summary>
    /// <typeparam name="T">The type of value that is returned</typeparam>
    public class DataLoaderResult<T> : IDataLoaderResult<T>
    {
        private readonly Task<T> _result;

        /// <summary>
        /// Returns an instance which always returns the default value.
        /// </summary>
        internal static readonly DataLoaderResult<T> DefaultValue = new(default(T)!);

        /// <summary>
        /// Initializes a DataLoaderResult with the given asynchronous task
        /// </summary>
        public DataLoaderResult(Task<T> result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Initializes a DataLoaderResult with the given value
        /// </summary>
        public DataLoaderResult(T result)
        {
            _result = Task.FromResult(result);
        }

        /// <summary>
        /// Asynchronously executes the loader if it has not yet been executed; then returns the result
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        public Task<T> GetResultAsync(CancellationToken cancellationToken = default) => _result;

        async Task<object?> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => await GetResultAsync(cancellationToken).ConfigureAwait(false);
    }
}
