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
        /// Registers the service of type <paramref name="serviceType"/> with the dependency
        /// injection provider if a service of the same type (and of the same implementation type
        /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
        /// has not already been registered.
        /// </summary>
        IServiceRegister TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType);

        /// <summary>
        /// Registers the service of type <paramref name="serviceType"/> with the dependency
        /// injection provider if a service of the same type (and of the same implementation type
        /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
        /// has not already been registered.
        /// <br/><br/>
        /// With <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>, it is required
        /// that <paramref name="implementationFactory"/> is a strongly typed delegate with a return type
        /// of a specific implementation type.
        /// </summary>
        IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType);

        /// <inheritdoc cref="TryRegister(Type, Type, ServiceLifetime, RegistrationCompareMode)"/>
        IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType);

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
