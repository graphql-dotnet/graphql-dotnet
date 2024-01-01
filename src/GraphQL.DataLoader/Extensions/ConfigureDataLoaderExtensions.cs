using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL;

/// <summary>
/// Extension methods for <see cref="IConfigureDataLoader"/>.
/// </summary>
public static class ConfigureDataLoaderExtensions
{
    /// <summary>
    /// Adds a batch data loader to the GraphQL configuration.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TFetcher">The type of the batch fetcher.</typeparam>
    /// <param name="builder">The configuration builder for GraphQL.</param>
    /// <param name="maxBatchSize">Optional. The maximum number of items to include in a single batch. Defaults to int.MaxValue.</param>
    /// <param name="equalityComparer">Optional. The equality comparer to use for comparing keys. Defaults to null, indicating the default comparer for the key type.</param>
    /// <param name="defaultValue">Optional. The default value to use when a key does not exist. Defaults to the default value of type T.</param>
    /// <param name="serviceLifetime">Optional. Specifies the service lifetime of the data loader in the dependency injection container. Defaults to Singleton.</param>
    /// <returns>Returns the modified <see cref="IConfigureDataLoader"/> after adding the batch data loader.</returns>
    /// <remarks>
    /// The batch data loader is used to efficiently load data in batches based on the requested keys.
    /// It utilizes the provided fetcher of type <typeparamref name="TFetcher"/> to fetch the data.
    /// The data loader is registered in the dependency injection container as a singleton of type
    /// <see cref="IDataLoader{TKey, T}"/>, while <typeparamref name="TFetcher"/> is registered with
    /// the specified <paramref name="serviceLifetime"/>. This allows <see cref="IDataLoader{TKey, T}"/>
    /// to be safely injected and used in any graph type within the GraphQL setup, regardless of the service
    /// lifetime of <typeparamref name="TFetcher"/> or whether the execution strategy is serial or parallel.
    /// If <typeparamref name="TFetcher"/> is an abstract type such as an interface, you must register
    /// a concrete implementation of the fetcher in the dependency injection container with the proper
    /// service lifetime.
    /// </remarks>
    public static IConfigureDataLoader AddBatchDataLoader<TKey, T, TFetcher>(
        this IConfigureDataLoader builder,
        int maxBatchSize = int.MaxValue,
        IEqualityComparer<TKey>? equalityComparer = null,
        T defaultValue = default!,
        DI.ServiceLifetime serviceLifetime = DI.ServiceLifetime.Singleton)
        where TKey : notnull
        where TFetcher : class, IBatchFetcher<TKey, T>
    {
        if (maxBatchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize));
        if (serviceLifetime == DI.ServiceLifetime.Singleton)
        {
            builder.Services.Register<IDataLoader<TKey, T>, DIScopedBatchDataLoader<TKey, T, TFetcher>>(DI.ServiceLifetime.Singleton);
            builder.Services.Register(new DIScopedBatchDataLoader<TKey, T, TFetcher>.Configuration(maxBatchSize, equalityComparer, defaultValue));
            if (!typeof(TFetcher).IsAbstract && !typeof(TFetcher).IsGenericTypeDefinition)
            {
                builder.Services.TryRegister<TFetcher>(DI.ServiceLifetime.Singleton);
            }
        }
        else if (serviceLifetime == DI.ServiceLifetime.Scoped)
        {
            builder.Services.Register<IDataLoader<TKey, T>, DIBatchDataLoader<TKey, T, TFetcher>>(DI.ServiceLifetime.Singleton);
            builder.Services.Register(new DIBatchDataLoader<TKey, T, TFetcher>.Configuration(maxBatchSize, equalityComparer, defaultValue));
            if (!typeof(TFetcher).IsAbstract && !typeof(TFetcher).IsGenericTypeDefinition)
            {
                builder.Services.TryRegister<TFetcher>(DI.ServiceLifetime.Scoped);
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(serviceLifetime), "Only singleton and scoped service lifetimes are registered.");
        }
        return builder;
    }

    private class DIBatchDataLoader<TKey, T, TFetcher> : DataLoaderBase<TKey, T>
        where TKey : notnull
        where TFetcher : IBatchFetcher<TKey, T>
    {
        private readonly TFetcher _fetcher;
        private readonly T _defaultValue;

        public DIBatchDataLoader(Configuration options, TFetcher fetcher)
            : base(false, options.Comparer, options.MaxBatchSize)
        {
            _fetcher = fetcher;
            _defaultValue = options.DefaultValue;
        }

        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, T>> list, CancellationToken cancellationToken)
        {
            var keys = list.Select(x => x.Key);
            var dictionary = await _fetcher.FetchAsync(keys, cancellationToken).ConfigureAwait(false);
            foreach (var item in list)
            {
                if (!dictionary.TryGetValue(item.Key, out var value))
                    value = _defaultValue;
                item.SetResult(value);
            }
        }

        public record Configuration(int MaxBatchSize, IEqualityComparer<TKey>? Comparer, T DefaultValue);
    }

    private class DIScopedBatchDataLoader<TKey, T, TFetcher> : DataLoaderBase<TKey, T>
        where TKey : notnull
        where TFetcher : IBatchFetcher<TKey, T>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly T _defaultValue;

        public DIScopedBatchDataLoader(Configuration options, IServiceScopeFactory serviceScopeFactory)
            : base(false, options.Comparer, options.MaxBatchSize)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _defaultValue = options.DefaultValue;
        }

        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, T>> list, CancellationToken cancellationToken)
        {
            var scope = _serviceScopeFactory.CreateScope();
            try
            {
                var fetcher = scope.ServiceProvider.GetRequiredService<TFetcher>();
                var keys = list.Select(x => x.Key);
                var dictionary = await fetcher.FetchAsync(keys, cancellationToken).ConfigureAwait(false);
                foreach (var item in list)
                {
                    if (!dictionary.TryGetValue(item.Key, out var value))
                        value = _defaultValue;
                    item.SetResult(value);
                }
            }
            finally
            {
                if (scope is IAsyncDisposable d)
                    await d.DisposeAsync().ConfigureAwait(false);
                else
                    scope.Dispose();
            }
        }

        public record Configuration(int MaxBatchSize, IEqualityComparer<TKey>? Comparer, T DefaultValue);
    }

    /// <summary>
    /// Adds a collection batch data loader to the GraphQL configuration.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TFetcher">The type of the collection batch fetcher.</typeparam>
    /// <param name="builder">The configuration builder for GraphQL.</param>
    /// <param name="maxBatchSize">Optional. The maximum number of items to include in a single batch. Defaults to int.MaxValue.</param>
    /// <param name="equalityComparer">Optional. The equality comparer to use for comparing keys. Defaults to null, indicating the default comparer for the key type.</param>
    /// <param name="serviceLifetime">Optional. Specifies the service lifetime of the data loader in the dependency injection container. Defaults to Singleton.</param>
    /// <returns>Returns the modified <see cref="IConfigureDataLoader"/> after adding the collection batch data loader.</returns>
    /// <remarks>
    /// The collection batch data loader is used to efficiently load data in batches based on the requested keys.
    /// It utilizes the provided fetcher of type <typeparamref name="TFetcher"/> to fetch the data.
    /// The data loader is registered in the dependency injection container as a singleton of type
    /// <see cref="IDataLoader{TKey, T}">IDataLoader&lt;TKey, IEnumerable&lt;T&gt;&gt;</see>, while <typeparamref name="TFetcher"/> is registered with
    /// the specified <paramref name="serviceLifetime"/>. This allows <see cref="IDataLoader{TKey, T}">IDataLoader&lt;TKey, IEnumerable&lt;T&gt;&gt;</see>
    /// to be safely injected and used in any graph type within the GraphQL setup.
    /// </remarks>
    public static IConfigureDataLoader AddCollectionBatchDataLoader<TKey, T, TFetcher>(
        this IConfigureDataLoader builder,
        int maxBatchSize = int.MaxValue,
        IEqualityComparer<TKey>? equalityComparer = null,
        DI.ServiceLifetime serviceLifetime = DI.ServiceLifetime.Singleton)
        where TKey : notnull
        where TFetcher : class, ICollectionBatchFetcher<TKey, T>
    {
        if (maxBatchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize));

        var config = new DICollectionBatchDataLoader<TKey, T, TFetcher>.Configuration(maxBatchSize, equalityComparer);

        switch (serviceLifetime)
        {
            case DI.ServiceLifetime.Singleton:
                builder.Services.Register<IDataLoader<TKey, IEnumerable<T>>, DICollectionBatchDataLoader<TKey, T, TFetcher>>(serviceLifetime);
                builder.Services.Register<TFetcher>(serviceLifetime);
                builder.Services.Register(config);
                break;
            case DI.ServiceLifetime.Scoped:
                builder.Services.Register<IDataLoader<TKey, IEnumerable<T>>, DICollectionBatchDataLoader<TKey, T, TFetcher>>(DI.ServiceLifetime.Scoped);
                builder.Services.Register<TFetcher>(DI.ServiceLifetime.Scoped);
                builder.Services.Register(config);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), "Only singleton and scoped service lifetimes are registered.");
        }

        return builder;
    }

    private class DICollectionBatchDataLoader<TKey, T, TFetcher> : DataLoaderBase<TKey, IEnumerable<T>>
        where TKey : notnull
        where TFetcher : ICollectionBatchFetcher<TKey, T>
    {
        private readonly TFetcher _fetcher;

        public DICollectionBatchDataLoader(Configuration options, TFetcher fetcher)
            : base(false, options.Comparer, options.MaxBatchSize)
        {
            _fetcher = fetcher;
        }

        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, IEnumerable<T>>> list, CancellationToken cancellationToken)
        {
            var keys = list.Select(x => x.Key);
            var collections = await _fetcher.FetchAsync(keys, cancellationToken).ConfigureAwait(false);
            foreach (var item in list)
            {
                item.SetResult(collections[item.Key]);
            }
        }

        public record Configuration(int MaxBatchSize, IEqualityComparer<TKey>? Comparer);
    }

    private class DIScopedCollectionBatchDataLoader<TKey, T, TFetcher> : DataLoaderBase<TKey, IEnumerable<T>>
        where TKey : notnull
        where TFetcher : ICollectionBatchFetcher<TKey, T>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public DIScopedCollectionBatchDataLoader(Configuration options, IServiceScopeFactory serviceScopeFactory)
            : base(false, options.Comparer, options.MaxBatchSize)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task FetchAsync(IEnumerable<DataLoaderPair<TKey, IEnumerable<T>>> list, CancellationToken cancellationToken)
        {
            var scope = _serviceScopeFactory.CreateScope();
            try
            {
                var fetcher = scope.ServiceProvider.GetRequiredService<TFetcher>();
                var keys = list.Select(x => x.Key);
                var collections = await fetcher.FetchAsync(keys, cancellationToken).ConfigureAwait(false);
                foreach (var item in list)
                {
                    item.SetResult(collections[item.Key]);
                }
            }
            finally
            {
                if (scope is IAsyncDisposable d)
                    await d.DisposeAsync().ConfigureAwait(false);
                else
                    scope.Dispose();
            }
        }

        public record Configuration(int MaxBatchSize, IEqualityComparer<TKey>? Comparer);
    }

}
