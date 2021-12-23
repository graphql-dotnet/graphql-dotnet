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
        /// Optionally removes any existing implementation of the same service type.
        /// </summary>
        IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime, bool)"/>
        IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime, bool)"/>
        IGraphQLBuilder Register(Type serviceType, object implementationInstance, bool replace = false);

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
        /// Configures an options class of type <typeparamref name="TOptions"/>. Each registration call to this method
        /// will be applied to instance of <typeparamref name="TOptions"/> returned from the DI engine.
        /// <br/><br/>
        /// If <paramref name="action"/> is <see langword="null"/> then <typeparamref name="TOptions"/> is still configured and
        /// will return a default instance (unless otherwise configured with a subsequent call to <see cref="Configure{TOptions}(Action{TOptions, IServiceProvider}?)">Configure</see>).
        /// </summary>
        IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
            where TOptions : class, new();
    }
}
