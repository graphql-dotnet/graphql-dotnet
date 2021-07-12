using System;

namespace GraphQL.DI
{
    /// <summary>
    /// An interface for configuring GraphQL.NET services.
    /// </summary>
    public interface IGraphQLBuilder
    {
        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider.
        /// </summary>
        IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime)"/>
        IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime)"/>
        IGraphQLBuilder Register(Type serviceType, object implementationInstance);

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="TryRegister(Type, Type, ServiceLifetime)"/>
        IGraphQLBuilder TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="TryRegister(Type, Type, ServiceLifetime)"/>
        IGraphQLBuilder TryRegister(Type serviceType, object implementationInstance);

        /// <summary>
        /// Configures an options class of type <typeparamref name="TOptions"/>.
        /// <br/><br/>
        /// Passing <see langword="null"/> as the delegate is allowed and will skip this registration.
        /// </summary>
        IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();
    }
}
