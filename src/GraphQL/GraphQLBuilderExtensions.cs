using System;
using System.Linq;
using System.Reflection;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Utilities;

namespace GraphQL
{
    public static class GraphQLBuilderExtensions
    {
        public static void Register<TService>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            => graphQLBuilder.Register(typeof(TService), typeof(TService), serviceLifetime);

        public static void Register<TService, TImplementation>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class
            => graphQLBuilder.Register(typeof(TService), typeof(TImplementation), serviceLifetime);

        public static void TryRegister<TService>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            => graphQLBuilder.TryRegister(typeof(TService), typeof(TService), serviceLifetime);

        public static void TryRegister<TService, TImplementation>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class
            => graphQLBuilder.TryRegister(typeof(TService), typeof(TImplementation), serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => builder.AddSchema<TSchema, GraphQLExecuter<TSchema>>(serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema, TGraphQLExecuter>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
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
            builder.Register<TSchema>(serviceLifetime);
            // Register ISchema with the DI provider if none already registered
            builder.TryRegister<ISchema>(serviceLifetime, services => services.GetRequiredService<TSchema>());
            // Register IGraphQLExecuter<TSchema> with the DI provider
            builder.Register<IGraphQLExecuter<TSchema>, TGraphQLExecuter>(serviceLifetime);
            // Register IGraphQLExecuter with the DI provider if none already registered
            builder.TryRegister<IGraphQLExecuter>(serviceLifetime, services => services.GetRequiredService<IGraphQLExecuter<TSchema>>());
            return builder;
        }

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, TSchema schema, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => AddSchema(builder, _ => schema, serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchema> schemaFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => AddSchema<TSchema, GraphQLExecuter<TSchema>>(builder, schemaFactory, serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema, TGraphQLExecuter>(this IGraphQLBuilder builder, TSchema schema, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            where TGraphQLExecuter : class, IGraphQLExecuter<TSchema>
            => AddSchema<TSchema, TGraphQLExecuter>(builder, _ => schema, serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema, TGraphQLExecuter>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchema> schemaFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
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
            builder.Register(serviceLifetime, schemaFactory);
            // Register ISchema with the DI provider if none already registered
            builder.TryRegister<ISchema>(serviceLifetime, services => services.GetRequiredService<TSchema>());
            // Register IGraphQLExecuter<TSchema> with the DI provider
            builder.Register<IGraphQLExecuter<TSchema>, TGraphQLExecuter>(serviceLifetime);
            // Register IGraphQLExecuter with the DI provider if none already registered
            builder.TryRegister<IGraphQLExecuter>(serviceLifetime, services => services.GetRequiredService<IGraphQLExecuter<TSchema>>());
            return builder;
        }

        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder)
            where TDocumentExecuter : class, IDocumentExecuter
        {
            builder.Register<IDocumentExecuter, TDocumentExecuter>(ServiceLifetime.Singleton);
            return builder;
        }

        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, ErrorInfoProviderOptions options)
            => AddErrorInfoProvider(builder, _ => options);

        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Func<IServiceProvider, ErrorInfoProviderOptions> optionsFactory)
        {
            builder.TryRegister<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);
            builder.Register(ServiceLifetime.Singleton, optionsFactory);
            return builder;
        }

        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder)
            where TProvider : class, IErrorInfoProvider
        {
            builder.Register<IErrorInfoProvider, TProvider>(ServiceLifetime.Singleton);
            return builder;
        }

        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, TProvider errorInfoProvider)
            where TProvider : class, IErrorInfoProvider
            => AddErrorInfoProvider(builder, _ => errorInfoProvider);

        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TProvider> errorInfoProviderFactory)
            where TProvider : class, IErrorInfoProvider
        {
            builder.Register<IErrorInfoProvider>(ServiceLifetime.Singleton, errorInfoProviderFactory);
            return builder;
        }

        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder)
            => builder.AddGraphTypes(Assembly.GetCallingAssembly());

        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder, Assembly assembly)
        {
            // Graph types are always created with the transient lifetime, since they are only instianted once
            // each time the schema is built. If the schema is a scoped schema, the graph types will effectively
            // be scoped graph types. If the schema is a singleton schema, the graph types will effectively be
            // singleton graph types. This is REQUIRED behavior and must not be changed.

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && typeof(IGraphType).IsAssignableFrom(x)))
            {
                builder.TryRegister(type, type, ServiceLifetime.Transient);
            }

            builder.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);

            return builder;
        }

        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Register<IDocumentExecutionListener, TDocumentListener>(ServiceLifetime.Transient);
            return builder;
        }

        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder)
            where TDocumentCache : class, IDocumentCache
        {
            builder.Register<IDocumentCache, TDocumentCache>(ServiceLifetime.Singleton);
            return builder;
        }

        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder, TDocumentCache documentCache)
            where TDocumentCache : class, IDocumentCache
            => AddDocumentCache(builder, _ => documentCache);

        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentCache> documentCacheFactory)
            where TDocumentCache : class, IDocumentCache
        {
            builder.Register<IDocumentCache>(ServiceLifetime.Singleton, documentCacheFactory);
            return builder;
        }

        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder)
            where TDocumentWriter : class, IDocumentWriter
        {
            builder.Register<IDocumentWriter, TDocumentWriter>(ServiceLifetime.Singleton);
            return builder;
        }

        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder, TDocumentWriter documentWriter)
            where TDocumentWriter : class, IDocumentWriter
            => AddDocumentWriter(builder, _ => documentWriter);

        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentWriter> documentWriterFactory)
            where TDocumentWriter : class, IDocumentWriter
        {
            builder.Register<IDocumentWriter>(ServiceLifetime.Singleton, documentWriterFactory);
            return builder;
        }
    }
}
