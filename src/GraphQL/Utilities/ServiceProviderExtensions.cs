using System;

namespace GraphQL.Utilities
{
    public static class ServiceProviderExtensions
    {
        public static T GetRequiredService<T>(this IServiceProvider provider)
        {
            return (T)GetRequiredService(provider, typeof(T));
        }

        public static object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof (provider));

            if (serviceType == (Type) null)
                throw new ArgumentNullException(nameof (serviceType));

            object service = provider.GetService(serviceType);
            if (service != null)
                return service;

            throw new InvalidOperationException($"Required service for type {serviceType} not found");
        }
    }
}
