#nullable enable

using System;
using GraphQL.DI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL.MicrosoftDI
{
    /// <inheritdoc cref="GraphQL.GraphQLBuilderExtensions"/>
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Configures a GraphQL pipeline using the configuration delegate passed into
        /// <paramref name="configure"/> for the specified service collection and
        /// registers a default set of services required by GraphQL if they have not already been registered.
        /// <br/><br/>
        /// Does not include <see cref="IDocumentWriter"/>, and the default <see cref="IDocumentExecuter"/>
        /// implementation does not support subscriptions.
        /// </summary>
        public static IServiceCollection AddGraphQL(this IServiceCollection services, Action<IGraphQLBuilder>? configure)
        {
            _ = new GraphQLBuilder(services, configure);
            return services;
        }

        /// <summary>
        /// Registers <typeparamref name="TSchema"/> within the dependency injection framework. <see cref="ISchema"/> is also
        /// registered if it is not already registered within the dependency injection framework. Services required by
        /// <typeparamref name="TSchema"/> are instantiated directly if not registered within the dependency injection framework.
        /// This can eliminate the need to register each of the graph types with the dependency injection framework, either
        /// manually or via <see cref="GraphQL.GraphQLBuilderExtensions.AddGraphTypes(IGraphQLBuilder)"/>. Singleton and scoped
        /// lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Schemas that implement <see cref="IDisposable"/> of a transient lifetime are not supported, as this will cause a
        /// memory leak if requested from the root service provider.
        /// </remarks>
        public static IGraphQLBuilder AddSelfActivatingSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (serviceLifetime == ServiceLifetime.Transient && typeof(IDisposable).IsAssignableFrom(typeof(TSchema)))
            {
                // This scenario can cause a memory leak if the schema is requested from the root service provider.
                // If it was requested from a scoped provider, then there is no reason to register it as transient.
                // See following link:
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container
                throw new InvalidOperationException("A schema that implements IDisposable should not be registered as a transient service. " +
                    "See https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container");
            }

            // Register the service with the DI provider as TSchema, overwriting any existing registration
            builder.Services.Register(provider =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(provider);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            }, serviceLifetime);

            // Now register the service as ISchema if not already registered.
            builder.Services.TryRegister<ISchema>(provider =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(provider);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            }, serviceLifetime);

            return builder;
        }
    }
}
