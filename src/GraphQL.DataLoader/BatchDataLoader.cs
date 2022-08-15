namespace GraphQL.DataLoader;

/// <summary>
/// A data loader that returns a single value for each given unique key
/// </summary>
/// <typeparam name="TKey">The type of the key</typeparam>
/// <typeparam name="T">The type of the return value</typeparam>
public class BatchDataLoader<TKey, T> : DataLoaderBase<TKey, T>
{
    private readonly Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> _loader;
    private readonly T _defaultValue;

    /// <summary>
    /// Initializes a new instance of BatchDataLoader with the specified fetch delegate
    /// </summary>
    /// <param name="fetchDelegate">An asynchronous delegate that is passed a list of keys and cancellation token, which returns a dictionary of keys and values</param>
    /// <param name="keyComparer">An optional equality comparer for the keys</param>
    /// <param name="defaultValue">The value returned when no match is found in the dictionary, or default(T) if unspecified</param>
    /// <param name="maxBatchSize">The maximum number of keys passed to the fetch delegate at a time</param>
    public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, T>>> fetchDelegate,
           IEqualityComparer<TKey>? keyComparer = null,
           T defaultValue = default!,
           int maxBatchSize = int.MaxValue) : base(keyComparer, maxBatchSize)
    {
        _loader = fetchDelegate ?? throw new ArgumentNullException(nameof(fetchDelegate));
        _defaultValue = defaultValue;
    }

    /// <summary>
    /// Initializes a new instance of BatchDataLoader with the specified fetch delegate and key selector
    /// </summary>
    /// <param name="fetchDelegate">An asynchronous delegate that is passed a list of keys and a cancellation token, which returns a list objects</param>
    /// <param name="keySelector">A selector for the key from the returned object</param>
    /// <param name="keyComparer">An optional equality comparer for the keys</param>
    /// <param name="defaultValue">The value returned when no match is found in the list, or default(T) if unspecified</param>
    /// <param name="maxBatchSize">The maximum number of keys passed to the fetch delegate at a time</param>
    public BatchDataLoader(Func<IEnumerable<TKey>, CancellationToken, Task<IEnumerable<T>>> fetchDelegate,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? keyComparer = null,
        T defaultValue = default!,
        int maxBatchSize = int.MaxValue) : base(keyComparer, maxBatchSize)
    {
        if (fetchDelegate == null)
            throw new ArgumentNullException(nameof(fetchDelegate));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        _loader = async (keys, cancellationToken) =>
        {
            var ret = await fetchDelegate(keys, cancellationToken).ConfigureAwait(false);
            return ret.ToDictionary(keySelector, keyComparer);
        };
        _defaultValue = defaultValue;
    }

    /// <inheritdoc/>
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, T>> list, CancellationToken cancellationToken)
    {
        var keys = list.Select(x => x.Key);
        var dictionary = await _loader(keys, cancellationToken).ConfigureAwait(false);
        foreach (var item in list)
        {
            if (!dictionary.TryGetValue(item.Key, out var value))
                value = _defaultValue;
            item.SetResult(value);
        }
    }
}
