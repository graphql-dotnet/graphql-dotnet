namespace GraphQL.DataLoader
{
    /// <summary>
    /// Represents a pending operation that can return a value
    /// </summary>
    /// <typeparam name="T">The type of value that is returned</typeparam>
    public interface IDataLoaderResult<T> : IDataLoaderResult
    {
        /// <summary>
        /// Asynchronously executes the loader if it has not yet been executed; then returns the result
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        new Task<T> GetResultAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a pending operation that can return a value
    /// </summary>
    public interface IDataLoaderResult
    {
        /// <summary>
        /// Asynchronously executes the loader if it has not yet been executed; then returns the result
        /// </summary>
        /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
        Task<object?> GetResultAsync(CancellationToken cancellationToken = default);
    }
}
