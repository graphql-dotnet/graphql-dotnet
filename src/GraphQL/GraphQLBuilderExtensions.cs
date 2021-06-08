using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Caching;
using GraphQL.DI;
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

            // Now register default implementations of ISchema and IGraphQLExecuter. These default implementation registrations
            // overwrite previous default implementations, such as the error message registered by default.
            builder.Register<IDefaultService<ISchema>>(ServiceLifetime.Transient, serviceProvider => new DefaultService<ISchema>(serviceProvider.GetRequiredService<TSchema>()));
            return builder;
        }

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, TSchema schema, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
            => AddSchema(builder, _ => schema, serviceLifetime);

        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchema> schemaFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
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
            builder.Register(serviceLifetime, schemaFactory);

            // Now register default implementations of ISchema and IGraphQLExecuter. These default implementation registrations
            // overwrite previous default implementations, such as the error message registered by default.
            builder.Register<IDefaultService<ISchema>>(ServiceLifetime.Transient, serviceProvider => new DefaultService<ISchema>(serviceProvider.GetRequiredService<TSchema>()));
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
            builder.Register<IErrorInfoProvider, ErrorInfoProvider>(ServiceLifetime.Singleton);
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

        public static IGraphQLBuilder AddClrMappings(this IGraphQLBuilder builder)
            => builder.AddClrMappings(Assembly.GetCallingAssembly());

        public static IGraphQLBuilder AddClrMappings(this IGraphQLBuilder builder, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var typesToRegister = new Type[]
            {
                typeof(ObjectGraphType<>),
                typeof(InputObjectGraphType<>),
                typeof(EnumerationGraphType<>),
            };

            var types = assembly
                .GetTypes()
                .Where(x => !x.IsAbstract && !x.IsInterface);

            var typeMappings = new List<(Type clrType, Type graphType)>();
            foreach (var graphType in types)
            {
                //skip types marked with the DoNotRegister attribute
                if (graphType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotRegisterAttribute)))
                    continue;
                //get the base type
                var baseType = graphType.BaseType;
                while (baseType != null)
                {
                    //skip types marked with the DoNotRegister attribute
                    if (baseType.GetCustomAttributes(false).Any(y => y.GetType() == typeof(DoNotRegisterAttribute)))
                        break;
                    //look for generic types that match our list above
                    if (baseType.IsConstructedGenericType && typesToRegister.Contains(baseType.GetGenericTypeDefinition()))
                    {
                        //get the base type
                        var clrType = baseType.GetGenericArguments()[0];
                        //and register it
                        if (clrType != typeof(object))
                            typeMappings.Add((clrType, graphType));
                        //skip to the next type
                        break;
                    }
                    //look up the inheritance chain for a match
                    baseType = baseType.BaseType;
                }
            }

            builder.AddSchemaConfiguration((serviceProvider, schema) =>
            {
                foreach (var typeMapping in typeMappings)
                {
                    schema.RegisterTypeMapping(typeMapping.clrType, typeMapping.graphType);
                }
            });

            return builder;
        }

        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Register<TDocumentListener>(serviceLifetime);
            builder.Register<IDocumentExecutionListener>(ServiceLifetime.Transient, services => services.GetRequiredService<TDocumentListener>());
            builder.AddExecutionConfiguration(options => options.Listeners.Add(options.RequestServices.GetRequiredService<TDocumentListener>()));
            return builder;
        }

        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, TDocumentListener documentListener)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Register(ServiceLifetime.Transient, _ => documentListener);
            builder.Register<IDocumentExecutionListener>(ServiceLifetime.Transient, _ => documentListener);
            builder.AddExecutionConfiguration(options => options.Listeners.Add(documentListener));
            return builder;
        }

        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, bool install = true, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Register<TMiddleware>(serviceLifetime);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Transient, services => services.GetRequiredService<TMiddleware>());
            if (install)
                builder.AddSchemaConfiguration((serviceProvider, schema) => schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>()));
            return builder;
        }

        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, Func<IServiceProvider, ISchema, bool> installPredicate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            // service lifetime is transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Register<TMiddleware>(serviceLifetime);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Transient, services => services.GetRequiredService<TMiddleware>());
            builder.AddSchemaConfiguration((serviceProvider, schema) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>());
            });
            return builder;
        }

        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, bool install = true)
            where TMiddleware : class, IFieldMiddleware
        {
            builder.Register(ServiceLifetime.Singleton, _ => middleware);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Singleton, _ => middleware);
            if (install)
                builder.AddSchemaConfiguration((serviceProvider, schema) => schema.FieldMiddleware.Use(middleware));
            return builder;
        }

        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, Func<IServiceProvider, ISchema, bool> installPredicate)
            where TMiddleware : class, IFieldMiddleware
        {
            builder.Register(ServiceLifetime.Singleton, _ => middleware);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Singleton, _ => middleware);
            builder.AddSchemaConfiguration((serviceProvider, schema) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(middleware);
            });
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

        public static IGraphQLBuilder AddSchemaConfiguration(this IGraphQLBuilder builder, Action<IServiceProvider, ISchema> configuration)
        {
            builder.Register(ServiceLifetime.Singleton, _ => configuration);
            return builder;
        }

        public static IGraphQLBuilder AddExecutionConfiguration(this IGraphQLBuilder builder, Action<ExecutionOptions> configuration)
        {
            builder.Register(ServiceLifetime.Singleton, _ => configuration);
            return builder;
        }

        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, bool enable = true)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>();
            if (enable)
                builder.AddExecutionConfiguration(options => options.EnableMetrics = true);
            return builder;
        }

        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enablePredicate)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>();
            builder.AddExecutionConfiguration(options =>
            {
                if (enablePredicate(options))
                {
                    options.EnableMetrics = true;
                }
            });
            return builder;
        }

        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enablePredicate, Func<IServiceProvider, ISchema, bool> installPredicate)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>(installPredicate);
            builder.AddExecutionConfiguration(options =>
            {
                if (enablePredicate(options))
                {
                    options.EnableMetrics = true;
                }
            });
            return builder;
        }
    }
}
