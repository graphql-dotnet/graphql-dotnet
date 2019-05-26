using DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFunc<TService>(this IServiceCollection services, bool validateParallelScopes = false)
        {
            return services.AddSingleton(provider =>
            {
                return new Func<TService>(() =>
                {
                    var scopeProviders = provider.GetRequiredService<IEnumerable<IScopeProvider>>();

                    foreach (var scopeProvider in scopeProviders)
                    {
                        var scopedServiceProvider = scopeProvider.GetScopedServiceProvider(provider);

                        if (scopedServiceProvider != null)
                        {
                            if (validateParallelScopes)
                            {
                                var parallelScopeProviders = scopeProviders.Except(new[] { scopeProvider }).Where(p => p.GetScopedServiceProvider(provider) != null).ToList();
                                if (parallelScopeProviders.Count > 0)
                                {
                                    parallelScopeProviders.Add(scopeProvider);
                                    throw new InvalidOperationException($"When the {nameof(validateParallelScopes)} option is enabled the simultaneous existence of several scopes from different providers is detected." + Environment.NewLine +
                                                                        $"Received scopes from the following providers: {string.Join(", ", parallelScopeProviders.Select(p => p.GetType().Name))}");
                                }
                            }

                            return scopedServiceProvider.GetRequiredService<TService>();
                        }
                    }

                    bool scopeRequired = services.FirstOrDefault(s => s.ServiceType == typeof(TService))?.Lifetime == ServiceLifetime.Scoped;
                    if (scopeRequired)
                    {
                        throw new InvalidOperationException($"An error occurred while resolving dependency {typeof(TService).Name}." + Environment.NewLine +
                                                             "The service is declared for Scoped scope within the context of the request, but an attempt to resolve the dependency is done outside the context of the request." + Environment.NewLine +
                                                             "An application can simultaneously have several entry points that form their request contexts." + Environment.NewLine +
                                                             "Be sure to add the correct context provider (IScopeProvider) to the container.");
                    }

                    return provider.GetRequiredService<TService>();
                });
            });
        }
    }
}
