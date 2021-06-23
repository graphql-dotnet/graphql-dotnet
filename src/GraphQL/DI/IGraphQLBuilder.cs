using System;

namespace GraphQL.DI
{
    /// <summary>
    /// An interface for configuring GraphQL.NET services.
    /// </summary>
    public interface IGraphQLBuilder
    {
        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
        /// </summary>
        IGraphQLBuilder Register<TService>(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class;

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider.
        /// </summary>
        IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IGraphQLBuilder TryRegister<TService>(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
            where TService : class;

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <summary>
        /// Configures an options class of type <typeparamref name="TOptions"/>.
        /// <br/><br/>
        /// Passing <see langword="null"/> as the delegate is allowed and will skip this registration.
        /// </summary>
        IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();
    }
}
