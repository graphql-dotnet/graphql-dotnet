namespace GraphQL.DI
{
    /// <summary>
    /// An interface for registering services with the dependency injection provider.
    /// </summary>
    public interface IServiceRegister
    {
        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider.
        /// Optionally removes any existing implementation of the same service type.
        /// When not replacing existing registrations, requesting the service type should return the most recent registration,
        /// and requesting an <see cref="IEnumerable{T}"/> of the service type should return all of the registrations.
        /// </summary>
        IServiceRegister Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime, bool)"/>
        IServiceRegister Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false);

        /// <inheritdoc cref="Register(Type, Type, ServiceLifetime, bool)"/>
        IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = false);

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// </summary>
        IServiceRegister TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="TryRegister(Type, Type, ServiceLifetime)"/>
        IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime);

        /// <inheritdoc cref="TryRegister(Type, Type, ServiceLifetime)"/>
        IServiceRegister TryRegister(Type serviceType, object implementationInstance);

        /// <summary>
        /// Configures an options class of type <typeparamref name="TOptions"/>. Each registration call to this method
        /// will be applied to instance of <typeparamref name="TOptions"/> returned from the DI engine.
        /// <br/><br/>
        /// If <paramref name="action"/> is <see langword="null"/> then <typeparamref name="TOptions"/> is still configured and
        /// will return a default instance (unless otherwise configured with a subsequent call to <see cref="Configure{TOptions}(Action{TOptions, IServiceProvider}?)">Configure</see>).
        /// </summary>
        IServiceRegister Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
            where TOptions : class, new();
    }
}
