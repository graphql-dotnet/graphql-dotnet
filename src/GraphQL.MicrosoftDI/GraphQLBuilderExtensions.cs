using System;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
            => new GraphQLBuilder(services);

        public static IGraphQLBuilder AddSelfActivatingSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (serviceLifetime == ServiceLifetime.Transient && typeof(IDisposable).IsAssignableFrom(typeof(TSchema)))
            {
                // This scenario can cause a memory leak if the schema is requested from the root service provider.
                // If it was requested from a scoped provider, then there is no reason to register it as transient.
                // See following link:
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container
                throw new InvalidOperationException("A schema that implements IDisposable cannot be registered as a transient service.");
            }
            
            // Register the service with the DI provider as TSchema, overwriting any existing registration
            builder.Register(serviceLifetime, services =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(services);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            });

            // Now register the service as ISchema if not already registered.
            builder.TryRegister<ISchema>(serviceLifetime, services =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(services);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            });

            return builder;
        }
    }
}
