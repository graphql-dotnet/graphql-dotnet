namespace GraphQL.DataLoader;

/// <summary>
/// Provides an IDataLoader that always returns the same data
/// </summary>
/// <typeparam name="T">The type of data that is returned</typeparam>
public class SimpleDataLoader<T> : IDataLoader, IDataLoader<T>, IDataLoaderResult<T>
{
    private readonly Func<CancellationToken, Task<T>> _fetchDelegate;
    private Task<T>? _result;

    /// <summary>
    /// Initializes a new SimpleDataLoader with the given fetch delegate
    /// </summary>
    /// <param name="fetchDelegate">An asynchronous delegate that accepts a cancellation token and returns data</param>
    public SimpleDataLoader(Func<CancellationToken, Task<T>> fetchDelegate)
    {
        _fetchDelegate = fetchDelegate ?? throw new ArgumentNullException(nameof(fetchDelegate));
    }

    /// <summary>
    /// Asynchronously executes the fetch delegate if it has not already been run
    /// </summary>
    /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
    public Task DispatchAsync(CancellationToken cancellationToken = default) => GetResultAsync(cancellationToken);

    /// <summary>
    /// Asynchronously executes the fetch delegate if it has not already been run, then returns the data
    /// </summary>
    /// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to pass to fetch delegate</param>
    public Task<T> GetResultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_result != null)
            return _result;

#pragma warning disable RCS1059 // Avoid locking on publicly accessible instance.
        lock (this)
#pragma warning restore RCS1059 // Avoid locking on publicly accessible instance.
        {
            if (_result != null)
                return _result;

            try
            {
                return (_result = _fetchDelegate(cancellationToken));
            }
            catch (Exception ex)
            {
                _result = Task.FromException<T>(ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Asynchronously load data
    /// </summary>
    /// <returns>
    /// An object representing a pending operation.
    /// </returns>
    public IDataLoaderResult<T> LoadAsync() => this;

    async Task<object?> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken)
        => await GetResultAsync(cancellationToken).ConfigureAwait(false);
}
