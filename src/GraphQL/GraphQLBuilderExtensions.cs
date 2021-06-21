using System;
using System.Linq;
using System.Reflection;
using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods to configure GraphQL.NET services within a dependency injection framework.
    /// </summary>
    public static class GraphQLBuilderExtensions
    {
        #region - Additional overloads for Register, TryRegister, ConfigureDefaults and Configure -
        /// <inheritdoc cref="IGraphQLBuilder.Register{TService}(ServiceLifetime, Func{IServiceProvider, TService})"/>
        public static IGraphQLBuilder Register<TService>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            => graphQLBuilder.Register(typeof(TService), typeof(TService), serviceLifetime);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
        /// An instance of <typeparamref name="TImplementation"/> will be created when an instance is needed.
        /// </summary>
        public static IGraphQLBuilder Register<TService, TImplementation>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => graphQLBuilder.Register(typeof(TService), typeof(TImplementation), serviceLifetime);

        /// <inheritdoc cref="IGraphQLBuilder.TryRegister{TService}(ServiceLifetime, Func{IServiceProvider, TService})"/>
        public static IGraphQLBuilder TryRegister<TService>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            => graphQLBuilder.TryRegister(typeof(TService), typeof(TService), serviceLifetime);

        /// <summary>
        /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
        /// of the same type has not already been registered.
        /// An instance of <typeparamref name="TImplementation"/> will be created when an instance is needed.
        /// </summary>
        public static IGraphQLBuilder TryRegister<TService, TImplementation>(this IGraphQLBuilder graphQLBuilder, ServiceLifetime serviceLifetime)
            where TService : class
            where TImplementation : class, TService
            => graphQLBuilder.TryRegister(typeof(TService), typeof(TImplementation), serviceLifetime);

        /// <inheritdoc cref="IGraphQLBuilder.Configure{TOptions}(Action{TOptions, IServiceProvider})"/>
        public static IGraphQLBuilder Configure<TOptions>(this IGraphQLBuilder graphQLBuilder, Action<TOptions> action)
            where TOptions : class, new()
            => graphQLBuilder.Configure<TOptions>(action == null ? null : (opt, _) => action(opt));

        /// <inheritdoc cref="IGraphQLBuilder.ConfigureDefaults{TOptions}(Action{TOptions, IServiceProvider})"/>
        public static IGraphQLBuilder ConfigureDefaults<TOptions>(this IGraphQLBuilder graphQLBuilder, Action<TOptions> action)
            where TOptions : class, new()
            => graphQLBuilder.ConfigureDefaults<TOptions>(action == null ? null : (opt, _) => action(opt));
        #endregion

        #region - AddSchema -
        /// <summary>
        /// Registers <typeparamref name="TSchema"/> within the dependency injection framework. <see cref="ISchema"/> is also
        /// registered if it is not already registered within the dependency injection framework. Singleton and scoped
        /// lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Schemas that implement <see cref="IDisposable"/> of a transient lifetime are not supported, as this will cause a
        /// memory leak if requested from the root service provider.
        /// </remarks>
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

            // Now register the service as ISchema if not already registered.
            builder.TryRegister<ISchema, TSchema>(serviceLifetime);

            return builder;
        }

        /// <summary>
        /// Registers <paramref name="schema"/> within the dependency injection framework as <typeparamref name="TSchema"/>. <see cref="ISchema"/> is also
        /// registered if it is not already registered within the dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, TSchema schema)
            where TSchema : class, ISchema
            => schema == null ? throw new ArgumentNullException(nameof(schema)) : AddSchema(builder, _ => schema, ServiceLifetime.Singleton);

        /// <inheritdoc cref="AddSchema{TSchema}(IGraphQLBuilder, ServiceLifetime)"/>
        public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchema> schemaFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (schemaFactory == null)
                throw new ArgumentNullException(nameof(schemaFactory));

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

            // Now register the service as ISchema if not already registered.
            builder.TryRegister<ISchema>(serviceLifetime, schemaFactory);

            return builder;
        }
        #endregion

        #region - AddDocumentExecuter -
        /// <summary>
        /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder)
            where TDocumentExecuter : class, IDocumentExecuter
            => builder.Register<IDocumentExecuter, TDocumentExecuter>(ServiceLifetime.Singleton);

        /// <summary>
        /// Registers <paramref name="documentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, TDocumentExecuter documentExecuter)
            where TDocumentExecuter : class, IDocumentExecuter
            => documentExecuter == null ? throw new ArgumentNullException(nameof(documentExecuter)) : builder.Register<IDocumentExecuter>(ServiceLifetime.Singleton, _ => documentExecuter);

        /// <summary>
        /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
        /// dependency injection framework. The supplied factory method is used to create the document executer.
        /// </summary>
        public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentExecuter> documentExecuterFactory)
            where TDocumentExecuter : class, IDocumentExecuter
            => builder.Register<IDocumentExecuter>(ServiceLifetime.Singleton, documentExecuterFactory ?? throw new ArgumentNullException(nameof(documentExecuterFactory)));
        #endregion

        #region - AddComplexityAnalyzer -
        /// <summary>
        /// Configures the default complexity analyzer with the specified configuration delegate.
        /// </summary>
        /// <remarks>
        /// Calling this method with a configuration delegate will overwrite any value passed to
        /// <see cref="IDocumentExecuter.ExecuteAsync(ExecutionOptions)"/> within
        /// <see cref="ExecutionOptions.ComplexityConfiguration"/> with a new instance configured by this call.
        /// </remarks>
        public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityConfiguration> action = null)
            => action != null ? builder.Configure(action) : builder;

        /// <inheritdoc cref="AddComplexityAnalyzer(IGraphQLBuilder, Action{ComplexityConfiguration})"/>
        public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityConfiguration, IServiceProvider> action)
            => action != null ? builder.Configure(action) : builder;

        /// <summary>
        /// Registers <typeparamref name="TAnalyzer"/> as a singleton of type <see cref="IComplexityAnalyzer"/> within the
        /// dependency injection framework and configures it with the specified configuration delegate.
        /// </summary>
        /// <remarks>
        /// Calling this method with a configuration delegate will overwrite any value passed to
        /// <see cref="IDocumentExecuter.ExecuteAsync(ExecutionOptions)"/> within
        /// <see cref="ExecutionOptions.ComplexityConfiguration"/> with a new instance configured by this call.
        /// </remarks>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Action<ComplexityConfiguration> action = null)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Register<IComplexityAnalyzer, TAnalyzer>(ServiceLifetime.Singleton);
            if (action != null)
                builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, Action{ComplexityConfiguration})"/>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Action<ComplexityConfiguration, IServiceProvider> action)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Register<IComplexityAnalyzer, TAnalyzer>(ServiceLifetime.Singleton);
            if (action != null)
                builder.Configure(action);
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="analyzer"/> as a singleton of type <see cref="IComplexityAnalyzer"/> within the
        /// dependency injection framework and configures it with the specified configuration delegate.
        /// </summary>
        /// <remarks>
        /// Calling this method with a configuration delegate will overwrite any value passed to
        /// <see cref="IDocumentExecuter.ExecuteAsync(ExecutionOptions)"/> within
        /// <see cref="ExecutionOptions.ComplexityConfiguration"/> with a new instance configured by this call.
        /// </remarks>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, TAnalyzer analyzer, Action<ComplexityConfiguration> action = null)
            where TAnalyzer : class, IComplexityAnalyzer
            => analyzer == null ? throw new ArgumentNullException(nameof(analyzer)) : AddComplexityAnalyzer(builder, _ => analyzer, action);

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, TAnalyzer, Action{ComplexityConfiguration})"/>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, TAnalyzer analyzer, Action<ComplexityConfiguration, IServiceProvider> action)
            where TAnalyzer : class, IComplexityAnalyzer
            => analyzer == null ? throw new ArgumentNullException(nameof(analyzer)) : AddComplexityAnalyzer(builder, _ => analyzer, action);

        /// <summary>
        /// Registers a singleton of type <see cref="IComplexityAnalyzer"/> within the dependency injection framework
        /// using the specified factory delegate, and configures it with the specified configuration delegate.
        /// </summary>
        /// <remarks>
        /// Calling this method with a configuration delegate will overwrite any value passed to
        /// <see cref="IDocumentExecuter.ExecuteAsync(ExecutionOptions)"/> within
        /// <see cref="ExecutionOptions.ComplexityConfiguration"/> with a new instance configured by this call.
        /// </remarks>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Func<IServiceProvider, TAnalyzer> analyzerFactory, Action<ComplexityConfiguration> action = null)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Register<IComplexityAnalyzer>(ServiceLifetime.Singleton, analyzerFactory ?? throw new ArgumentNullException(nameof(analyzerFactory)));
            if (action != null)
                builder.Configure(action);
            return builder;
        }

        /// <inheritdoc cref="AddComplexityAnalyzer{TAnalyzer}(IGraphQLBuilder, Func{IServiceProvider, TAnalyzer}, Action{ComplexityConfiguration})"/>
        public static IGraphQLBuilder AddComplexityAnalyzer<TAnalyzer>(this IGraphQLBuilder builder, Func<IServiceProvider, TAnalyzer> analyzerFactory, Action<ComplexityConfiguration, IServiceProvider> action)
            where TAnalyzer : class, IComplexityAnalyzer
        {
            builder.Register<IComplexityAnalyzer>(ServiceLifetime.Singleton, analyzerFactory ?? throw new ArgumentNullException(nameof(analyzerFactory)));
            if (action != null)
                builder.Configure(action);
            return builder;
        }
        #endregion

        #region - AddErrorInfoProvider
        /// <summary>
        /// Configures the default error info provider with the specified configuration delegate.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions> action = null)
            => builder.AddErrorInfoProvider<ErrorInfoProvider>().Configure(action);

        /// <inheritdoc cref="AddErrorInfoProvider(IGraphQLBuilder, Action{ErrorInfoProviderOptions})"/>
        public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions, IServiceProvider> action)
            => builder.AddErrorInfoProvider<ErrorInfoProvider>().Configure(action);

        /// <summary>
        /// Registers <typeparamref name="TProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder)
            where TProvider : class, IErrorInfoProvider
            => builder.Register<IErrorInfoProvider, TProvider>(ServiceLifetime.Singleton);

        /// <summary>
        /// Registers <paramref name="errorInfoProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, TProvider errorInfoProvider)
            where TProvider : class, IErrorInfoProvider
            => errorInfoProvider == null ? throw new ArgumentNullException(nameof(errorInfoProvider)) : AddErrorInfoProvider(builder, _ => errorInfoProvider);

        /// <summary>
        /// Registers <see cref="IErrorInfoProvider"/> within the dependency injection framework using the supplied
        /// factory delegate.
        /// </summary>
        public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TProvider> errorInfoProviderFactory)
            where TProvider : class, IErrorInfoProvider
            => builder.Register<IErrorInfoProvider>(ServiceLifetime.Singleton, errorInfoProviderFactory);
        #endregion

        #region - AddGraphTypes -
        /// <summary>
        /// Scans the calling assembly for classes that implement <see cref="IGraphType"/> and registers
        /// them as transients within the dependency injection framework. A transient lifetime ensures
        /// they are only instianted once each time the schema is built. If the schema is a scoped schema,
        /// the graph types will effectively be scoped graph types. If the schema is a singleton schema,
        /// the graph types will effectively be singleton graph types.
        /// <br/><br/>
        /// Also registers <see cref="EnumerationGraphType{TEnum}"/>, <see cref="ConnectionType{TNodeType}"/>,
        /// <see cref="ConnectionType{TNodeType, TEdgeType}"/>, <see cref="EdgeType{TNodeType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>, and
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> as generic types.
        /// </summary>
        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder)
            => builder.AddGraphTypes(Assembly.GetCallingAssembly());

        /// <summary>
        /// Scans the supplied assembly for classes that implement <see cref="IGraphType"/> and registers
        /// them as transients within the dependency injection framework. A transient lifetime ensures
        /// they are only instianted once each time the schema is built. If the schema is a scoped schema,
        /// the graph types will effectively be scoped graph types. If the schema is a singleton schema,
        /// the graph types will effectively be singleton graph types.
        /// <br/><br/>
        /// Also registers <see cref="EnumerationGraphType{TEnum}"/>, <see cref="ConnectionType{TNodeType}"/>,
        /// <see cref="ConnectionType{TNodeType, TEdgeType}"/>, <see cref="EdgeType{TNodeType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>, and
        /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> as generic types.
        /// </summary>
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
            builder.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
            builder.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);

            return builder;
        }
        #endregion

        #region - AddClrTypeMappings -
        /// <summary>
        /// Scans the calling assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="SchemaExtensions.RegisterTypeMappings(ISchema)"/>
        /// within the schema constructor.
        /// </remarks>
        public static IGraphQLBuilder AddClrTypeMappings(this IGraphQLBuilder builder)
            => builder.AddClrTypeMappings(Assembly.GetCallingAssembly());

        /// <summary>
        /// Scans the specified assembly for classes that inherit from <see cref="ObjectGraphType{TSourceType}"/>,
        /// <see cref="InputObjectGraphType{TSourceType}"/>, or <see cref="EnumerationGraphType{TEnum}"/>, and
        /// registers clr type mappings on the schema between that class and the source type or underlying enum type.
        /// Skips classes where the source type is <see cref="object"/>, or where the class is marked with
        /// the <see cref="DoNotMapClrTypeAttribute"/>.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="SchemaExtensions.RegisterTypeMappings(ISchema, Assembly)"/>
        /// within the schema constructor.
        /// </remarks>
        public static IGraphQLBuilder AddClrTypeMappings(this IGraphQLBuilder builder, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            // retreive all of the type mappings ahead-of-time, in case of a scoped or transient schema,
            // as reflection is relatively slow
            var typeMappings = assembly.GetClrTypeMappings();
            builder.ConfigureSchema((schema, serviceProvider) =>
            {
                foreach (var typeMapping in typeMappings)
                {
                    schema.RegisterTypeMapping(typeMapping.ClrType, typeMapping.GraphType);
                }
            });

            return builder;
        }
        #endregion

        #region - AddDocumentListener -
        /// <summary>
        /// Registers <typeparamref name="TDocumentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>. Configures document execution to add an instance of <typeparamref name="TDocumentListener"/> to the
        /// list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>. Singleton, scoped and transient lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Register<TDocumentListener>(serviceLifetime);
            builder.Register<IDocumentExecutionListener>(serviceLifetime);
            builder.ConfigureExecution(options => options.Listeners.Add(options.RequestServices.GetRequiredService<TDocumentListener>()));
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="documentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>. Configures document execution to add <paramref name="documentListener"/> to the
        /// list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, TDocumentListener documentListener)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            if (documentListener == null)
                throw new ArgumentNullException(nameof(documentListener));

            builder.Register(ServiceLifetime.Singleton, _ => documentListener);
            builder.Register<IDocumentExecutionListener>(ServiceLifetime.Singleton, _ => documentListener);
            builder.ConfigureExecution(options => options.Listeners.Add(documentListener));
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TDocumentListener"/> with the dependency injection framework as both <typeparamref name="TDocumentListener"/> and
        /// <see cref="IDocumentExecutionListener"/>, using the supplied factory delegate. Configures document execution to add an instance of
        /// <typeparamref name="TDocumentListener"/> to the list of document execution listeners within <see cref="ExecutionOptions.Listeners"/>.
        /// Singleton, scoped and transient lifetimes are supported.
        /// </summary>
        /// <remarks>
        /// Do not separately add the document listener to your execution code or the document listener may be registered twice for the same execution.
        /// </remarks>
        public static IGraphQLBuilder AddDocumentListener<TDocumentListener>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentListener> documentListenerFactory, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TDocumentListener : class, IDocumentExecutionListener
        {
            builder.Register(serviceLifetime, documentListenerFactory ?? throw new ArgumentNullException(nameof(documentListenerFactory)));
            builder.Register<IDocumentExecutionListener>(serviceLifetime, documentListenerFactory);
            builder.ConfigureExecution(options => options.Listeners.Add(options.RequestServices.GetRequiredService<TDocumentListener>()));
            return builder;
        }
        #endregion

        #region - AddMiddleware -
        /// <summary>
        /// Registers <typeparamref name="TMiddleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. If <paramref name="install"/> is <see langword="true"/>, installs the middleware by configuring schema
        /// construction to call <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance
        /// of the middleware pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// If <paramref name="install"/> is <see langword="true"/>, do not separately install the middleware within your schema constructor or the
        /// middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, bool install = true, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            if (serviceLifetime == ServiceLifetime.Scoped)
            {
                // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
                throw new InvalidOperationException("Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
            }

            // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Register<TMiddleware>(serviceLifetime);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Transient, services => services.GetRequiredService<TMiddleware>());
            if (install)
                builder.ConfigureSchema((schema, serviceProvider) => schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>()));
            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TMiddleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. Calls the <paramref name="installPredicate"/> delegate during schema construction, and if
        /// <see langword="true"/>, installs the middleware by configuring schema construction to call
        /// <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance of the middleware
        /// pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// Do not separately install the middleware within your schema constructor or the middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, Func<IServiceProvider, ISchema, bool> installPredicate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IFieldMiddleware
        {
            if (installPredicate == null)
                throw new ArgumentNullException(nameof(installPredicate));

            if (serviceLifetime == ServiceLifetime.Scoped)
            {
                // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
                throw new InvalidOperationException("Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
            }

            // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
            builder.Register<TMiddleware>(serviceLifetime);
            builder.Register<IFieldMiddleware>(serviceLifetime);
            builder.ConfigureSchema((schema, serviceProvider) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(serviceProvider.GetRequiredService<TMiddleware>());
            });
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="middleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. If <paramref name="install"/> is <see langword="true"/>, installs the middleware by configuring schema
        /// construction to call <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance
        /// of the middleware pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// If <paramref name="install"/> is <see langword="true"/>, do not separately install the middleware within your schema constructor or the
        /// middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, bool install = true)
            where TMiddleware : class, IFieldMiddleware
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            builder.Register(ServiceLifetime.Singleton, _ => middleware);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Singleton, _ => middleware);
            if (install)
                builder.ConfigureSchema((schema, serviceProvider) => schema.FieldMiddleware.Use(middleware));
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="middleware"/> with the dependency injection framework as both <typeparamref name="TMiddleware"/> and
        /// <see cref="IFieldMiddleware"/>. Calls the <paramref name="installPredicate"/> delegate during schema construction, and if
        /// <see langword="true"/>, installs the middleware by configuring schema construction to call
        /// <see cref="FieldMiddlewareBuilderExtensions.Use(IFieldMiddlewareBuilder, IFieldMiddleware)">Use</see> with an instance of the middleware
        /// pulled from dependency injection. Transient and singleton lifetimes are supported. Transient is default, and causes the middleware
        /// lifetime to match that of the schema. This effectively provides singleton middleware if using a singleton schema, and scoped middleware
        /// if using a scoped schema. Specifying a singleton lifetime is also permissible, providing a benefit if the schema has a scoped lifetime.
        /// </summary>
        /// <remarks>
        /// Do not separately install the middleware within your schema constructor or the middleware may be registered twice within the schema.
        /// </remarks>
        public static IGraphQLBuilder AddMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, Func<IServiceProvider, ISchema, bool> installPredicate)
            where TMiddleware : class, IFieldMiddleware
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            if (installPredicate == null)
                throw new ArgumentNullException(nameof(installPredicate));

            builder.Register(ServiceLifetime.Singleton, _ => middleware);
            builder.Register<IFieldMiddleware>(ServiceLifetime.Singleton, _ => middleware);
            builder.ConfigureSchema((schema, serviceProvider) =>
            {
                if (installPredicate(serviceProvider, schema))
                    schema.FieldMiddleware.Use(middleware);
            });
            return builder;
        }
        #endregion

        #region - AddDocumentCache -
        /// <summary>
        /// Registers <typeparamref name="TDocumentCache"/> as a singleton of type <see cref="IDocumentCache"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder)
            where TDocumentCache : class, IDocumentCache
            => builder.Register<IDocumentCache, TDocumentCache>(ServiceLifetime.Singleton);

        /// <summary>
        /// Registers <paramref name="documentCache"/> as a singleton of type <see cref="IDocumentCache"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder, TDocumentCache documentCache)
            where TDocumentCache : class, IDocumentCache
            => documentCache == null ? throw new ArgumentNullException(nameof(documentCache)) : AddDocumentCache(builder, _ => documentCache);

        /// <summary>
        /// Registers <typeparamref name="TDocumentCache"/> as a singleton of type <see cref="IDocumentCache"/> within the
        /// dependency injection framework. The supplied factory method is used to create the document cache.
        /// </summary>
        public static IGraphQLBuilder AddDocumentCache<TDocumentCache>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentCache> documentCacheFactory)
            where TDocumentCache : class, IDocumentCache
            => builder.Register<IDocumentCache>(ServiceLifetime.Singleton, documentCacheFactory ?? throw new ArgumentNullException(nameof(documentCacheFactory)));
        #endregion

        #region - AddDocumentWriter -
        /// <summary>
        /// Registers <typeparamref name="TDocumentWriter"/> as a singleton of type <see cref="IDocumentWriter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder)
            where TDocumentWriter : class, IDocumentWriter
            => builder.Register<IDocumentWriter, TDocumentWriter>(ServiceLifetime.Singleton);

        /// <summary>
        /// Registers <paramref name="documentWriter"/> as a singleton of type <see cref="IDocumentWriter"/> within the
        /// dependency injection framework.
        /// </summary>
        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder, TDocumentWriter documentWriter)
            where TDocumentWriter : class, IDocumentWriter
            => documentWriter == null ? throw new ArgumentNullException(nameof(documentWriter)) : AddDocumentWriter(builder, _ => documentWriter);

        /// <summary>
        /// Registers <typeparamref name="TDocumentWriter"/> as a singleton of type <see cref="IDocumentWriter"/> within the
        /// dependency injection framework. The supplied factory method is used to create the document writer.
        /// </summary>
        public static IGraphQLBuilder AddDocumentWriter<TDocumentWriter>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentWriter> documentWriterFactory)
            where TDocumentWriter : class, IDocumentWriter
            => builder.Register<IDocumentWriter>(ServiceLifetime.Singleton, documentWriterFactory ?? throw new ArgumentNullException(nameof(documentWriterFactory)));
        #endregion

        #region - ConfigureSchema and ConfigureExecution -
        /// <summary>
        /// Configures an action to run prior to the code within the schema's constructor.
        /// Assumes that the schema derives from <see cref="Schema"/>.
        /// </summary>
        public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema> action)
            => action == null ? throw new ArgumentNullException(nameof(action)) : builder.ConfigureSchema((schema, _) => action(schema));

        /// <inheritdoc cref="ConfigureSchema(IGraphQLBuilder, Action{ISchema})"/>
        public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema, IServiceProvider> action)
            => action == null ? throw new ArgumentNullException(nameof(action)) : builder.Register(ServiceLifetime.Singleton, _ => action);

        /// <summary>
        /// Configures an action to run immediately prior to document execution.
        /// Assumes that the document executer is <see cref="DocumentExecuter"/>, or that it derives from <see cref="DocumentExecuter"/> and calls
        /// <see cref="DocumentExecuter(IDocumentBuilder, IDocumentValidator, IComplexityAnalyzer, IDocumentCache, System.Collections.Generic.IEnumerable{Action{ExecutionOptions}})"/>
        /// within the constructor.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecutionOptions.RequestServices"/> can be used within the delegate to access the service provider for this execution.
        /// </remarks>
        public static IGraphQLBuilder ConfigureExecution(this IGraphQLBuilder builder, Action<ExecutionOptions> action)
            => action == null ? throw new ArgumentNullException(nameof(action)) : builder.Register(ServiceLifetime.Singleton, _ => action);
        #endregion

        #region - AddValidationRule -
        /// <summary>
        /// Registers <typeparamref name="TValidationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/>.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
        {
            builder.Register<TValidationRule>(ServiceLifetime.Singleton);
            builder.Register<IValidationRule, TValidationRule>(ServiceLifetime.Singleton);
            builder.ConfigureExecution(options =>
            {
                var rule = options.RequestServices.GetRequiredService<TValidationRule>();
                options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(rule);
                if (useForCachedDocuments)
                {
                    options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(rule);
                }
            });
            return builder;
        }

        /// <summary>
        /// Registers <paramref name="validationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/>.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, TValidationRule validationRule, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
            => validationRule == null ? throw new ArgumentNullException(nameof(validationRule)) : builder.AddValidationRule(_ => validationRule, useForCachedDocuments);

        /// <summary>
        /// Registers <typeparamref name="TValidationRule"/> as a singleton within the dependency injection framework
        /// as <typeparamref name="TValidationRule"/> and as <see cref="IValidationRule"/> using the specified factory delegate.
        /// Configures document execution to add the validation rule within <see cref="ExecutionOptions.ValidationRules"/>.
        /// When <paramref name="useForCachedDocuments"/> is <see langword="true"/>, also configures document execution to
        /// add the validation rule within <see cref="ExecutionOptions.CachedDocumentValidationRules"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="useForCachedDocuments"/> is <see langword="true"/>, do not separately install the validation rule within
        /// your execution code or the validation rule may be run twice for each execution.
        /// </remarks>
        public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, Func<IServiceProvider, TValidationRule> validationRuleFactory, bool useForCachedDocuments = false)
            where TValidationRule : class, IValidationRule
        {
            builder.Register(ServiceLifetime.Singleton, validationRuleFactory ?? throw new ArgumentNullException(nameof(validationRuleFactory)));
            builder.Register<IValidationRule>(ServiceLifetime.Singleton, validationRuleFactory);
            builder.ConfigureExecution(options =>
            {
                var rule = options.RequestServices.GetRequiredService<TValidationRule>();
                options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(rule);
                if (useForCachedDocuments)
                {
                    options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(rule);
                }
            });
            return builder;
        }
        #endregion

        #region - AddMetrics -
        /// <summary>
        /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
        /// configures it to be installed within the schema.
        /// When <paramref name="enable"/> is <see langword="true"/>, configures execution to set
        /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
        /// </summary>
        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, bool enable = true)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>();
            if (enable)
                builder.ConfigureExecution(options => options.EnableMetrics = true);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
        /// configures it to be installed within the schema.
        /// Configures execution to run <paramref name="enablePredicate"/> and when <see langword="true"/>, sets
        /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
        /// </summary>
        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enablePredicate)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>();
            builder.ConfigureExecution(options =>
            {
                if (enablePredicate(options))
                {
                    options.EnableMetrics = true;
                }
            });
            return builder;
        }

        /// <summary>
        /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
        /// configures it to be installed within the schema when <paramref name="installPredicate"/> returns <see langword="true"/>.
        /// Configures execution to run <paramref name="enablePredicate"/> and when <see langword="true"/>, sets
        /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
        /// </summary>
        public static IGraphQLBuilder AddMetrics(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enablePredicate, Func<IServiceProvider, ISchema, bool> installPredicate)
        {
            builder.AddMiddleware<InstrumentFieldsMiddleware>(installPredicate);
            builder.ConfigureExecution(options =>
            {
                if (enablePredicate(options))
                {
                    options.EnableMetrics = true;
                }
            });
            return builder;
        }
        #endregion
    }
}
