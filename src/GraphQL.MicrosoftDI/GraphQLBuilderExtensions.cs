using System;
using GraphQL.Caching;
using GraphQL.DI;
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
            services.TryAddTransient(services => services.GetService<IOptions<ComplexityConfiguration>>()?.Value); // Registering IOptions<ComplexityConfiguration> or registering ComplexityConfiguration will work
            services.TryAddTransient(services => services.GetService<IOptions<ErrorInfoProviderOptions>>()?.Value); // Registering IOptions<ErrorInfoProviderOptions> or registering ErrorInfoProviderOptions will work

            // configure default services which can be overridden by extension methods
            services.TryAddTransient<IDefaultService<IDocumentExecuter>, DefaultServiceFromDI<DocumentExecuter>>();
            services.TryAddTransient<IDefaultService<IDocumentBuilder>, DefaultServiceFromDI<GraphQLDocumentBuilder>>();
            services.TryAddTransient<IDefaultService<IDocumentValidator>, DefaultServiceFromDI<DocumentValidator>>();
            services.TryAddTransient<IDefaultService<IComplexityAnalyzer>, DefaultServiceFromDI<ComplexityAnalyzer>>();
            services.TryAddTransient<IDefaultService<IDocumentCache>>(_ => new DefaultService<IDocumentCache>(DefaultDocumentCache.Instance));
            services.TryAddTransient<IDefaultService<IErrorInfoProvider>, DefaultServiceFromDI<ErrorInfoProvider>>();

            // configure an error to be displayed for these service defaults
            services.TryAddTransient<IDefaultService<IDocumentWriter>>(_ =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            });
            services.TryAddTransient<IDefaultService<ISchema>>(_ =>
            {
                throw new InvalidOperationException(
                    "ISchema not set in DI container. " +
                    "Please use AddSchema or register an ISchema implementation in your DI container.");
            });
            services.TryAddTransient<IDefaultService<IGraphQLExecuter>>(_ =>
            {
                throw new InvalidOperationException(
                    "IGraphQLExecuter not set in DI container. " +
                    "Please use AddSchema or register an ISchema implementation in your DI container.");
            });

            // configure service implementations to use the configured default services when not overridden by a user
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IDocumentExecuter>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IDocumentBuilder>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IDocumentValidator>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IComplexityAnalyzer>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IDocumentCache>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IErrorInfoProvider>>().Instance);

            // these services have no default implementation initially
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IDocumentWriter>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<ISchema>>().Instance);
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IDefaultService<IGraphQLExecuter>>().Instance);

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
            // Also register IGraphQLExecuter<TSchema> with the DI provider, overwriting any existing registration
            builder.Register<IGraphQLExecuter<TSchema>, TGraphQLExecuter>(serviceLifetime);

            // Now register default implementations of ISchema and IGraphQLExecuter. These default implementation registrations
            // overwrite previous default implementations, such as the error message registered by default.
            builder.Register<IDefaultService<ISchema>>(ServiceLifetime.Transient, serviceProvider => new DefaultService<ISchema>(serviceProvider.GetRequiredService<TSchema>()));
            builder.Register<IDefaultService<IGraphQLExecuter>>(ServiceLifetime.Transient, serviceProvider => new DefaultService<IGraphQLExecuter>(serviceProvider.GetRequiredService<IGraphQLExecuter<TSchema>>()));
            return builder;
        }
    }
}
