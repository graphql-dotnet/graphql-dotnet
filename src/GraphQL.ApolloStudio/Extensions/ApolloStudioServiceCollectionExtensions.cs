using System;
using GraphQL.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.ApolloStudio.Extensions
{
    /// <summary>
    /// Service collection extensions for adding apollo studio trace logging
    /// </summary>
    public static class ApolloStudioServiceCollectionExtensions
    {
        /// <summary>Adds services required for routing requests.</summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
        /// <param name="configureOptions">The routing options to configure the middleware with.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddApolloStudioTraceLogging(this IServiceCollection services, Action<ApolloStudioTraceOptions> configureOptions) =>
            services
                .AddScoped<ITraceLogger, ApolloStudioTraceLogger>()
                .Configure(configureOptions);
    }
}
