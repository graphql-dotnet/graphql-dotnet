namespace GraphQL.DI
{
    /// <summary>
    /// Comparison mode used for <see cref="IServiceRegister.TryRegister(Type, Type, ServiceLifetime, RegistrationCompareMode)">IServiceRegister.TryRegister</see>
    /// methods.
    /// </summary>
    public enum RegistrationCompareMode
    {
        /// <summary>
        /// Registers the service with the dependency injection provider
        /// if a service of the same service type has not already been registered.
        /// </summary>
        ServiceType,

        /// <summary>
        /// Registers the service with the dependency injection provider
        /// if a service of the same service type and same implementation type
        /// has not already been registered.
        /// </summary>
        ServiceTypeAndImplementationType
    }
}
