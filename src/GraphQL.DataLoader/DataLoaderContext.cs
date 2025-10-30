using System.Collections.Concurrent;

namespace GraphQL.DataLoader;

/// <summary>
/// Provides a way to register DataLoader instances
/// </summary>
public class DataLoaderContext
{
    private ConcurrentDictionary<string, IDataLoader>? _loaders;

    private ConcurrentDictionary<string, IDataLoader> GetLoaders()
    {
        var loaders = _loaders;
        if (loaders == null)
        {
            // Atomically initialize _loaders if it's still null.
            // CompareExchange returns the original value of _loaders:
            //   - null if we won the initialization (and set _loaders = newLoaders)
            //   - the existing dictionary if another thread initialized it first
            // '?? newLoaders' makes 'loaders' reference whichever instance is now in _loaders.

            var newLoaders = new ConcurrentDictionary<string, IDataLoader>();
            loaders = Interlocked.CompareExchange(ref _loaders, newLoaders, null) ?? newLoaders;
        }
        return loaders;
    }

    /// <summary>
    /// Add a new data loader if one does not already exist with the provided key
    /// </summary>
    /// <typeparam name="TDataLoader">The type of <seealso cref="IDataLoader"/></typeparam>
    /// <param name="loaderKey">Unique string to identify the <seealso cref="IDataLoader"/> instance</param>
    /// <param name="dataLoaderFactory">Function to create the TDataLoader instance if it does not already exist</param>
    /// <returns>Returns an existing TDataLoader instance or a newly created instance if it did not exist already</returns>
    public TDataLoader GetOrAdd<TDataLoader>(string loaderKey, Func<TDataLoader> dataLoaderFactory)
        where TDataLoader : IDataLoader
    {
        if (loaderKey == null)
            throw new ArgumentNullException(nameof(loaderKey));

        if (dataLoaderFactory == null)
            throw new ArgumentNullException(nameof(dataLoaderFactory));

        return (TDataLoader)GetLoaders().GetOrAdd(loaderKey, _ => dataLoaderFactory());
    }

    /// <summary>
    /// Add a new data loader if one does not already exist with the provided key
    /// </summary>
    /// <typeparam name="TDataLoader">The type of <seealso cref="IDataLoader"/></typeparam>
    /// <typeparam name="TValue">The type of the factory argument</typeparam>
    /// <param name="loaderKey">Unique string to identify the <seealso cref="IDataLoader"/> instance</param>
    /// <param name="dataLoaderFactory">Function to create the TDataLoader instance if it does not already exist. The factory receives a value of type TValue as a parameter.</param>
    /// <param name="factoryArgument">The argument to pass to the factory function</param>
    /// <returns>Returns an existing TDataLoader instance or a newly created instance if it did not exist already</returns>
    public TDataLoader GetOrAdd<TDataLoader, TValue>(string loaderKey, Func<TValue, TDataLoader> dataLoaderFactory, TValue factoryArgument)
        where TDataLoader : IDataLoader
    {
        if (loaderKey == null)
            throw new ArgumentNullException(nameof(loaderKey));

        if (dataLoaderFactory == null)
            throw new ArgumentNullException(nameof(dataLoaderFactory));

#if NETSTANDARD2_1 || NETCOREAPP2_0_OR_GREATER
        return (TDataLoader)GetLoaders().GetOrAdd(loaderKey, static (_, x) => x.dataLoaderFactory(x.factoryArgument), (dataLoaderFactory, factoryArgument));
#else
        return (TDataLoader)GetLoaders().GetOrAdd(loaderKey, _ => dataLoaderFactory(factoryArgument));
#endif
    }
}
