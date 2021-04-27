using System;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GraphQL.MicrosoftDI
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<IDocumentBuilder, GraphQLDocumentBuilder>();
            services.TryAddSingleton<IDocumentValidator, DocumentValidator>();
            services.TryAddSingleton<IComplexityAnalyzer, ComplexityAnalyzer>();
            services.TryAddTransient(services => services.GetService<IOptions<ComplexityConfiguration>>()?.Value); // Registering IOptions<ComplexityConfiguration> or registering ComplexityConfiguration will work
            services.TryAddSingleton<IDocumentCache>(DefaultDocumentCache.Instance);
            services.TryAddSingleton<IErrorInfoProvider, ErrorInfoProvider>();
            services.TryAddTransient(services => services.GetService<IOptions<ErrorInfoProviderOptions>>()?.Value); // Registering IOptions<ErrorInfoProviderOptions> or registering ErrorInfoProviderOptions will work
            services.TryAddSingleton<IDocumentWriter>(x =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            });
            return new GraphQLBuilder(services);
        }

        public static IGraphQLBuilder AddSelfActivatingSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => builder.AddSelfActivatingSchema<TSchema, GraphQLExecuter<TSchema>>(serviceLifetime);

        public static IGraphQLBuilder AddSelfActivatingSchema<TSchema, TGraphQLExecuter>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            where TGraphQLExecuter : class, IGraphQLExecuter<TSchema>
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
            // Register ISchema with the DI provider if none already registered; maps to the TSchema registration
            builder.TryRegister<ISchema>(serviceLifetime, services => services.GetRequiredService<TSchema>());
            // Register IGraphQLExecuter<TSchema> with the DI provider
            builder.Register<IGraphQLExecuter<TSchema>, TGraphQLExecuter>(serviceLifetime);
            // Register IGraphQLExecuter with the DI provider if none already registered
            builder.TryRegister<IGraphQLExecuter>(serviceLifetime, services => services.GetRequiredService<IGraphQLExecuter<TSchema>>());
            return builder;
        }
    }
}
