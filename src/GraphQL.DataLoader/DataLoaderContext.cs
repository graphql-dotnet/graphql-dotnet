using System.Collections.Concurrent;

namespace GraphQL.DataLoader;

/// <summary>
/// Provides a way to register DataLoader instances
/// </summary>
public class DataLoaderContext
{
    private ConcurrentDictionary<string, IDataLoader>? _loaders;

    private ConcurrentDictionary<string, IDataLoader> GetLoaders()
        => LazyInitializer.EnsureInitialized(ref _loaders)!;

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
    /// <param name="factoryArgument">The argument to pass to the factory function</param>
    /// <param name="dataLoaderFactory">Function to create the TDataLoader instance if it does not already exist. The factory receives a value of type TValue as a parameter.</param>
    /// <returns>Returns an existing TDataLoader instance or a newly created instance if it did not exist already</returns>
    public TDataLoader GetOrAdd<TDataLoader, TValue>(string loaderKey, TValue factoryArgument, Func<TValue, TDataLoader> dataLoaderFactory)
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
