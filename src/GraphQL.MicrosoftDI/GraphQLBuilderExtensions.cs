using System;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.MicrosoftDI
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
        {
            services.TryAddSingleton<InstrumentFieldsMiddleware>();
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<IDocumentBuilder, GraphQLDocumentBuilder>();
            services.TryAddSingleton<IDocumentValidator, DocumentValidator>();
            services.TryAddSingleton<IComplexityAnalyzer, ComplexityAnalyzer>();
            services.TryAddSingleton<IDocumentCache>(DefaultDocumentCache.Instance);
            services.TryAddSingleton<IErrorInfoProvider, ErrorInfoProvider>();
            
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

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => builder.AddSchema<TSchema, GraphQLExecuter<TSchema>>(serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema, TGraphQLExecuter>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            where TGraphQLExecuter : class, IGraphQLExecuter<TSchema>
        {
            // Register the service with the DI provider as TSchema, overwriting any existing registration
            builder.Register(serviceLifetime, services =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(services);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            });
            // Register ISchema with the DI provider if none already registered
            builder.TryRegister<ISchema>(serviceLifetime, services => services.GetRequiredService<TSchema>());
            // Register IGraphQLExecuter<TSchema> with the DI provider
            builder.Register<IGraphQLExecuter<TSchema>, TGraphQLExecuter>(serviceLifetime);
            // Register IGraphQLExecuter with the DI provider if none already registered
            builder.TryRegister<IGraphQLExecuter>(serviceLifetime, services => services.GetRequiredService<IGraphQLExecuter<TSchema>>());
            return builder;
        }
    }
}
