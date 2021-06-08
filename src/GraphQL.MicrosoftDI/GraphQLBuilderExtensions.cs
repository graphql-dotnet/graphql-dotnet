using System;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.MicrosoftDI
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
        {
            var builder = new GraphQLBuilder(services);

            // configure mapping for IOptions<ComplexityConfiguation> and IOptions<ErrorInfoProviderOptions>
            // note that this code will cause a null to be passed into applicable constructor arguments during DI injection if these objects are unconfigured
            builder.TryRegister(ServiceLifetime.Transient, services => services.GetService<IOptions<ComplexityConfiguration>>()?.Value); // Registering IOptions<ComplexityConfiguration> or registering ComplexityConfiguration will work
            builder.TryRegister(ServiceLifetime.Transient, services => services.GetService<IOptions<ErrorInfoProviderOptions>>()?.Value); // Registering IOptions<ErrorInfoProviderOptions> or registering ErrorInfoProviderOptions will work

            // configure an error to be displayed when no IDocumentWriter is registered
            services.TryAddTransient<IDocumentWriter>(_ =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            });

            // configure service implementations to use the configured default services when not overridden by a user
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<IDocumentBuilder, GraphQLDocumentBuilder>();
            services.TryAddSingleton<IDocumentValidator, DocumentValidator>();
            services.TryAddSingleton<IComplexityAnalyzer, ComplexityAnalyzer>();
            services.TryAddSingleton<IDocumentCache>(DefaultDocumentCache.Instance);
            services.TryAddSingleton<IErrorInfoProvider, ErrorInfoProvider>();

            // configure an error message to be displayed if RequestServices is null,
            // and configure the ComplexityAnalyzer to be pulled from DI and configured (but left unchanged if not configured in DI)
            Action<ExecutionOptions> configureComplexityConfiguration = options =>
            {
                if (options.RequestServices == null)
                    throw new InvalidOperationException("Cannot execute request if RequestServices is null.");

                var complexityConfiguration = options.RequestServices.GetService<ComplexityConfiguration>();
                if (complexityConfiguration != null)
                    options.ComplexityConfiguration = complexityConfiguration;
            };
            services.AddSingleton(_ => configureComplexityConfiguration);

            return new GraphQLBuilder(services);
        }

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
