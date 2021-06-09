using System;

namespace GraphQL
{
    public interface IGraphQLBuilder
    {
        IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        IGraphQLBuilder TryRegister<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        IGraphQLBuilder ConfigureDefaults<TOptions>(Action<TOptions, IServiceProvider> optionsFactory)
            where TOptions : class, new();

        IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();
    }

    public enum ServiceLifetime
    {
        Singleton,
        Scoped,
        Transient,
    }
}
