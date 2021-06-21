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
        IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider.
        /// </summary>
        IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IGraphQLBuilder TryRegister<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class;

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <summary>
        /// Configures an options class of type <typeparamref name="TOptions"/>.
        /// <br/><br/>
        /// Delegates registered with <see cref="ConfigureDefaults{TOptions}(Action{TOptions, IServiceProvider})">ConfigureDefaults</see> are
        /// executed prior to delegates registered with <see cref="Configure{TOptions}(Action{TOptions, IServiceProvider})">Configure</see>.
        /// <br/><br/>
        /// Passing <see langword="null"/> as the delegate is allowed and will skip this registration.
        /// </summary>
        IGraphQLBuilder ConfigureDefaults<TOptions>(Action<TOptions, IServiceProvider> optionsFactory)
            where TOptions : class, new();

        /// <inheritdoc cref="ConfigureDefaults{TOptions}(Action{TOptions, IServiceProvider})"/>
        IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
            where TOptions : class, new();
    }
}
