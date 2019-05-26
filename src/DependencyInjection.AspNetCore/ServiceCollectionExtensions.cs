using DependencyInjection;
using DependencyInjection.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpScope(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            // TryAddEnumerable since the application may have several providers
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IScopeProvider, AspNetCoreHttpScopeProvider>());
            return services;
        }
    }
}
