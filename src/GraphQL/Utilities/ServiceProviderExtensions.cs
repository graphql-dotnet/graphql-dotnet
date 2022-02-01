namespace GraphQL.Utilities
{
    internal static class ServiceProviderExtensions
    {
        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// This method has exactly the same behavior as ServiceProviderServiceExtensions.GetRequiredService.
        /// It is added so as not to be dependent on the Microsoft.Extensions.DependencyInjection.Abstractions package.
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice
        /// </summary>
        public static T GetRequiredService<T>(this IServiceProvider provider) => (T)GetRequiredService(provider, typeof(T));

        /// <summary>
        /// Get service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
        /// This method has exactly the same behavior as ServiceProviderServiceExtensions.GetRequiredService.
        /// It is added so as not to be dependent on the Microsoft.Extensions.DependencyInjection.Abstractions package.
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceproviderserviceextensions.getrequiredservice
        /// </summary>
        public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            object? service = provider.GetService(serviceType);
            if (service != null)
                return service;

            throw new InvalidOperationException($"Required service for type {serviceType} not found");
        }
    }
}
