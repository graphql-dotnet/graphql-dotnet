using System.Reflection;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.PersistedDocuments;

#if NET5_0_OR_GREATER
using GraphQL.Telemetry;
#endif
using GraphQL.Types;
using GraphQL.Types.Collections;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser.AST;

namespace GraphQL;

/// <summary>
/// Provides extension methods to configure GraphQL.NET services within a dependency injection framework.
/// </summary>
public static class GraphQLBuilderExtensions // TODO: split
{
    // see matching list in IConfigureExecution.SortOrder xml comments
    internal const float SORT_ORDER_OPTIONS = 100;
    internal const float SORT_ORDER_CONFIGURATION = 200;

    #region - Additional overloads for Register, TryRegister and Configure -
    /// <inheritdoc cref="Register{TService}(IServiceRegister, Func{IServiceProvider, TService}, ServiceLifetime, bool)"/>
    public static IServiceRegister Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceRegister services, ServiceLifetime serviceLifetime, bool replace = false)
        where TService : class
        => services.Register(typeof(TService), typeof(TService), serviceLifetime, replace);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
    /// An instance of <typeparamref name="TImplementation"/> will be created when an instance is needed.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime, bool replace = false)
        where TService : class
        where TImplementation : class, TService
        => services.Register(typeof(TService), typeof(TImplementation), serviceLifetime, replace);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService>(this IServiceRegister services, Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
        where TService : class
        => services.Register(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime, replace);

    /// <summary>
    /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider.
    /// Optionally removes any existing implementation of the same service type.
    /// </summary>
    public static IServiceRegister Register<TService>(this IServiceRegister services, TService implementationInstance, bool replace = false)
        where TService : class
        => services.Register(typeof(TService), implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance)), replace);

    /// <inheritdoc cref="TryRegister{TService}(IServiceRegister, Func{IServiceProvider, TService}, ServiceLifetime)"/>
    public static IServiceRegister TryRegister<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceRegister services, ServiceLifetime serviceLifetime)
        where TService : class
        => services.TryRegister(typeof(TService), typeof(TService), serviceLifetime);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency
    /// injection provider if a service of the same type (and of the same implementation type
    /// in case of <see cref="RegistrationCompareMode.ServiceTypeAndImplementationType"/>)
    /// has not already been registered. An instance of <typeparamref name="TImplementation"/>
    /// will be created when an instance is needed.
    /// </summary>
    public static IServiceRegister TryRegister<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        where TService : class
        where TImplementation : class, TService
        => services.TryRegister(typeof(TService), typeof(TImplementation), serviceLifetime, mode);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
    /// of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        where TService : class
        where TImplementation : class, TService
        => services.TryRegister(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime, mode);

    /// <summary>
    /// Registers the service of type <typeparamref name="TService"/> with the dependency injection provider if a service
    /// of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService>(this IServiceRegister services, Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
        where TService : class
        => services.TryRegister(typeof(TService), implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory)), serviceLifetime);

    /// <summary>
    /// Registers <paramref name="implementationInstance"/> as type <typeparamref name="TService"/> with the dependency injection provider
    /// if a service of the same type has not already been registered.
    /// </summary>
    public static IServiceRegister TryRegister<TService>(this IServiceRegister services, TService implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        where TService : class
        => services.TryRegister(typeof(TService), implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance)), mode);

    /// <inheritdoc cref="IServiceRegister.Configure{TOptions}(Action{TOptions, IServiceProvider})"/>
    public static IServiceRegister Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>(this IServiceRegister services, Action<TOptions>? action)
        where TOptions : class, new()
        => services.Configure<TOptions>(action == null ? null : (opt, _) => action(opt));
    #endregion

    #region - RegisterAsBoth and TryRegisterAsBoth -
    /// <summary>
    /// Calls Register for both the implementation and service
    /// </summary>
    private static IServiceRegister RegisterAsBoth<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        => services.Register<TImplementation>(serviceLifetime).Register<TService, TImplementation>(serviceLifetime);

    /// <summary>
    /// Calls Register for both the implementation and service
    /// </summary>
    private static IServiceRegister RegisterAsBoth<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        => services.Register(implementationFactory, serviceLifetime).Register<TService>(implementationFactory, serviceLifetime);

    /// <summary>
    /// Calls Register for both the implementation and service
    /// </summary>
    private static IServiceRegister RegisterAsBoth<TService, TImplementation>(this IServiceRegister services, TImplementation implementationInstance)
        where TService : class
        where TImplementation : class, TService
        => services.Register(implementationInstance).Register<TService>(implementationInstance);

    /// <summary>
    /// Calls Register for the implementation and TryRegister for the service
    /// </summary>
    private static IServiceRegister TryRegisterAsBoth<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceRegister services, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        => services.Register<TImplementation>(serviceLifetime).TryRegister<TService, TImplementation>(serviceLifetime);

    /// <summary>
    /// Calls Register for the implementation and TryRegister for the service
    /// </summary>
    private static IServiceRegister TryRegisterAsBoth<TService, TImplementation>(this IServiceRegister services, Func<IServiceProvider, TImplementation> implementationFactory, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : class, TService
        => services.Register(implementationFactory, serviceLifetime).TryRegister<TService>(implementationFactory, serviceLifetime);

    /// <summary>
    /// Calls Register for the implementation and TryRegister for the service
    /// </summary>
    private static IServiceRegister TryRegisterAsBoth<TService, TImplementation>(this IServiceRegister services, TImplementation implementationInstance)
        where TService : class
        where TImplementation : class, TService
        => services.Register(implementationInstance).TryRegister<TService>(implementationInstance);
    #endregion

    #region - AddSchema -
    /// <summary>
    /// Registers <typeparamref name="TSchema"/> within the dependency injection framework. <see cref="ISchema"/> is also
    /// registered if it is not already registered within the dependency injection framework. Singleton and scoped
    /// lifetimes are supported. For scoped lifetimes, enables <see cref="GlobalSwitches.EnableReflectionCaching"/>.
    /// </summary>
    /// <remarks>
    /// Schemas that implement <see cref="IDisposable"/> of a transient lifetime are not supported, as this will cause a
    /// memory leak if requested from the root service provider.
    /// </remarks>
    public static IGraphQLBuilder AddSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
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
        // Also register the service as ISchema if not already registered.
        builder.Services.TryRegisterAsBoth<ISchema, TSchema>(serviceLifetime);

#if !DEBUG // otherwise any scoped service test would change the global switches
        if (serviceLifetime != ServiceLifetime.Singleton)
        {
            GlobalSwitches.EnableReflectionCaching = true;
            GlobalSwitches.DynamicallyCompileToObject = false;
        }
#endif

        return builder;
    }

    /// <summary>
    /// Registers <paramref name="schema"/> within the dependency injection framework as <typeparamref name="TSchema"/>. <see cref="ISchema"/> is also
    /// registered if it is not already registered within the dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddSchema<TSchema>(this IGraphQLBuilder builder, TSchema schema)
        where TSchema : class, ISchema
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        // Register the service with the DI provider as TSchema, overwriting any existing registration
        // Also register the service as ISchema if not already registered.
        builder.Services.TryRegisterAsBoth<ISchema, TSchema>(schema);

        return builder;
    }

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
            throw new InvalidOperationException("A schema that implements IDisposable should not be registered as a transient service. " +
                "See https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container");
        }

        // Register the service with the DI provider as TSchema, overwriting any existing registration
        // Also register the service as ISchema if not already registered.
        builder.Services.TryRegisterAsBoth<ISchema, TSchema>(schemaFactory, serviceLifetime);

#if !DEBUG // otherwise any scoped service test would change the global switches
        if (serviceLifetime != ServiceLifetime.Singleton)
        {
            GlobalSwitches.EnableReflectionCaching = true;
            GlobalSwitches.DynamicallyCompileToObject = false;
        }
#endif

        return builder;
    }
    #endregion

    #region - AddGraphTypeMappingProvider -
    /// <summary>
    /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
    /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/>.
    /// <br/><br/>
    /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
    /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
    /// types.
    /// </summary>
    public static IGraphQLBuilder AddGraphTypeMappingProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphTypeMappingProvider>(this IGraphQLBuilder builder)
        where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
    {
        builder.Services.Register<IGraphTypeMappingProvider, TGraphTypeMappingProvider>(ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
    /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> using the specified factory delegate.
    /// <br/><br/>
    /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
    /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
    /// types.
    /// </summary>
    public static IGraphQLBuilder AddGraphTypeMappingProvider<TGraphTypeMappingProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TGraphTypeMappingProvider> factory)
        where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
    {
        builder.Services.Register<IGraphTypeMappingProvider>(factory, ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers an instance of <typeparamref name="TGraphTypeMappingProvider"/> with the dependency injection
    /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> using the specified instance.
    /// <br/><br/>
    /// An <see cref="IGraphTypeMappingProvider"/> can be used to map one or more CLR types to graph types.
    /// For instance, unmapped CLR output types can be mapped to <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
    /// types.
    /// </summary>
    public static IGraphQLBuilder AddGraphTypeMappingProvider<TGraphTypeMappingProvider>(this IGraphQLBuilder builder, TGraphTypeMappingProvider instance)
        where TGraphTypeMappingProvider : class, IGraphTypeMappingProvider
    {
        builder.Services.Register<IGraphTypeMappingProvider>(instance);
        return builder;
    }
    #endregion

    #region - AddAutoSchema / WithMutation / WithSubscription -
    /// <summary>
    /// Registers an instance of the <see cref="AutoSchema{TQueryClrType}"/> class within the dependency injection framework as a singleton.
    /// <see cref="ISchema"/> is also registered if it is not already registered within the dependency injection framework.
    /// <see cref="Schema.Query"/> is set to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> with
    /// <typeparamref name="TQueryClrType"/> as TSourceType.
    /// <br/><br/>
    /// Additionally, this method calls <see cref="AddAutoClrMappings(IGraphQLBuilder, bool, bool)">AddAutoClrMappings</see>
    /// so that unmapped CLR input or output types are mapped to <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>
    /// and <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> respectively.
    /// <br/><br/>
    /// To register a mutation or subscription CLR type within the schema, use the <paramref name="configure"/> delegate and
    /// call <see cref="WithMutation{TMutationClrType}(IConfigureAutoSchema)">WithMutation</see> or
    /// <see cref="WithSubscription{TSubscriptionClrType}(IConfigureAutoSchema)">WithSubscription</see>, respsectively.
    /// <br/><br/>
    /// This allows for a schema that is entirely configured with CLR types.
    /// </summary>
    [RequiresUnreferencedCode("Please ensure that the CLR types used by your schema are not trimmed by the compiler.")]
    public static IGraphQLBuilder AddAutoSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TQueryClrType>(this IGraphQLBuilder builder, Action<IConfigureAutoSchema>? configure = null)
    {
        builder.AddSchema(provider => new AutoSchema<TQueryClrType>(provider), ServiceLifetime.Singleton);
        builder.Services.TryRegister<IGraphTypeMappingProvider, AutoRegisteringGraphTypeMappingProvider>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
        configure?.Invoke(new ConfigureAutoSchema<TQueryClrType>(builder));
        return builder;
    }

    /// <summary>
    /// Configures <see cref="Schema.Mutation"/> to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
    /// with <typeparamref name="TMutationClrType"/> as TSourceType.
    /// </summary>
    public static IConfigureAutoSchema WithMutation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TMutationClrType>(this IConfigureAutoSchema builder)
    {
        builder.Builder.ConfigureSchema((schema, provider) =>
        {
            if (schema.GetType() == builder.SchemaType)
                schema.Mutation = provider.GetRequiredService<AutoRegisteringObjectGraphType<TMutationClrType>>();
        });
        return builder;
    }

    /// <summary>
    /// Configures <see cref="Schema.Subscription"/> to an instance of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
    /// with <typeparamref name="TSubscriptionClrType"/> as TSourceType.
    /// </summary>
    public static IConfigureAutoSchema WithSubscription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TSubscriptionClrType>(this IConfigureAutoSchema builder)
    {
        builder.Builder.ConfigureSchema((schema, provider) =>
        {
            if (schema.GetType() == builder.SchemaType)
                schema.Subscription = provider.GetRequiredService<AutoRegisteringObjectGraphType<TSubscriptionClrType>>();
        });
        return builder;
    }
    #endregion

    #region - AddDocumentExecuter -
    /// <summary>
    /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
    /// dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddDocumentExecuter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDocumentExecuter>(this IGraphQLBuilder builder)
        where TDocumentExecuter : class, IDocumentExecuter
    {
        builder.Services.Register<IDocumentExecuter, TDocumentExecuter>(ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers <paramref name="documentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
    /// dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, TDocumentExecuter documentExecuter)
        where TDocumentExecuter : class, IDocumentExecuter
    {
        builder.Services.Register<IDocumentExecuter>(documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter)));
        return builder;
    }

    /// <summary>
    /// Registers <typeparamref name="TDocumentExecuter"/> as a singleton of type <see cref="IDocumentExecuter"/> within the
    /// dependency injection framework. The supplied factory method is used to create the document executer.
    /// </summary>
    public static IGraphQLBuilder AddDocumentExecuter<TDocumentExecuter>(this IGraphQLBuilder builder, Func<IServiceProvider, TDocumentExecuter> documentExecuterFactory)
        where TDocumentExecuter : class, IDocumentExecuter
    {
        builder.Services.Register<IDocumentExecuter>(documentExecuterFactory ?? throw new ArgumentNullException(nameof(documentExecuterFactory)), ServiceLifetime.Singleton);
        return builder;
    }
    #endregion

    #region - AddComplexityAnalyzer -
    /// <summary>
    /// Enables the legacy complexity analyzer and configures it with the specified configuration delegate.
    /// </summary>
    [Obsolete("Please use the new complexity analyzer. The v7 complexity analyzer will be removed in v9.")]
    public static IGraphQLBuilder AddLegacyComplexityAnalyzer(this IGraphQLBuilder builder, Action<LegacyComplexityConfiguration>? action = null)
    {
        builder.AddValidationRule<LegacyComplexityValidationRule>();
        builder.Services.Configure(action);
        return builder;
    }

    /// <inheritdoc cref="AddLegacyComplexityAnalyzer(IGraphQLBuilder, Action{LegacyComplexityConfiguration})"/>
    [Obsolete("Please use the new complexity analyzer. The v7 complexity analyzer will be removed in v9.")]
    public static IGraphQLBuilder AddLegacyComplexityAnalyzer(this IGraphQLBuilder builder, Action<LegacyComplexityConfiguration, IServiceProvider>? action)
    {
        builder.AddValidationRule<LegacyComplexityValidationRule>();
        builder.Services.Configure(action);
        return builder;
    }

    /// <summary>
    /// Enables the default complexity analyzer and configures it with the specified configuration delegate.
    /// </summary>
    public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityOptions>? action = null)
    {
        builder.AddValidationRule<ComplexityValidationRule>();
        builder.Services.Configure(action);
        return builder;
    }

    /// <inheritdoc cref="AddComplexityAnalyzer(IGraphQLBuilder, Action{ComplexityOptions})"/>
    public static IGraphQLBuilder AddComplexityAnalyzer(this IGraphQLBuilder builder, Action<ComplexityOptions, IServiceProvider>? action)
    {
        builder.AddValidationRule<ComplexityValidationRule>();
        builder.Services.Configure(action);
        return builder;
    }
    #endregion

    #region - AddErrorInfoProvider
    /// <summary>
    /// Configures the default error info provider with the specified configuration delegate.
    /// </summary>
    public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions>? action = null)
    {
        builder.Services.Configure(action);
        return builder.AddErrorInfoProvider<ErrorInfoProvider>();
    }

    /// <inheritdoc cref="AddErrorInfoProvider(IGraphQLBuilder, Action{ErrorInfoProviderOptions})"/>
    public static IGraphQLBuilder AddErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.AddErrorInfoProvider<ErrorInfoProvider>();
    }

    /// <summary>
    /// Registers <typeparamref name="TProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
    /// dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddErrorInfoProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(this IGraphQLBuilder builder)
        where TProvider : class, IErrorInfoProvider
    {
        builder.Services.Register<IErrorInfoProvider, TProvider>(ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers <paramref name="errorInfoProvider"/> as a singleton of type <see cref="IErrorInfoProvider"/> within the
    /// dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, TProvider errorInfoProvider)
        where TProvider : class, IErrorInfoProvider
    {
        if (errorInfoProvider == null)
            throw new ArgumentNullException(nameof(errorInfoProvider));

        builder.Services.Register<IErrorInfoProvider>(errorInfoProvider);
        return builder;
    }

    /// <summary>
    /// Registers <see cref="IErrorInfoProvider"/> within the dependency injection framework using the supplied
    /// factory delegate.
    /// </summary>
    public static IGraphQLBuilder AddErrorInfoProvider<TProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TProvider> errorInfoProviderFactory)
        where TProvider : class, IErrorInfoProvider
    {
        if (errorInfoProviderFactory == null)
            throw new ArgumentNullException(nameof(errorInfoProviderFactory));

        builder.Services.Register<IErrorInfoProvider>(errorInfoProviderFactory, ServiceLifetime.Singleton);
        return builder;
    }
    #endregion

    #region - AddGraphTypes -
    /// <summary>
    /// Scans the calling assembly for classes that implement <see cref="IGraphType"/> and registers
    /// them as transients within the dependency injection framework. A transient lifetime ensures
    /// they are only instantiated once each time the schema is built. If the schema is a scoped schema,
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
    /// they are only instantiated once each time the schema is built. If the schema is a scoped schema,
    /// the graph types will effectively be scoped graph types. If the schema is a singleton schema,
    /// the graph types will effectively be singleton graph types.
    /// <br/><br/>
    /// Skips classes where the class is marked with the <see cref="DoNotRegisterAttribute"/>.
    /// <br/><br/>
    /// Also registers <see cref="EnumerationGraphType{TEnum}"/>, <see cref="ConnectionType{TNodeType}"/>,
    /// <see cref="ConnectionType{TNodeType, TEdgeType}"/>, <see cref="EdgeType{TNodeType}"/>,
    /// <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/>, and
    /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> as generic types.
    /// </summary>
    public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder, Assembly assembly)
    {
        // Graph types are always created with the transient lifetime, since they are only instantiated once
        // each time the schema is built. If the schema is a scoped schema, the graph types will effectively
        // be scoped graph types. If the schema is a singleton schema, the graph types will effectively be
        // singleton graph types. This is REQUIRED behavior and must not be changed.

        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        foreach (var type in assembly.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && typeof(IGraphType).IsAssignableFrom(x) && !x.IsDefined(typeof(DoNotRegisterAttribute))))
        {
            builder.Services.TryRegister(type, type, ServiceLifetime.Transient);
        }

        builder.Services.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient);
        builder.Services.TryRegister<PageInfoType>(ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient);
        builder.Services.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient);

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
    [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
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
    [RequiresUnreferencedCode("Please ensure that the graph types used by your schema and their constructors are not trimmed by the compiler.")]
    public static IGraphQLBuilder AddClrTypeMappings(this IGraphQLBuilder builder, Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        // retreive all of the type mappings ahead-of-time, in case of a scoped or transient schema,
        // as reflection is relatively slow
        var typeMappings = assembly.GetClrTypeMappings();
        foreach (var typeMapping in typeMappings)
        {
            builder.AddGraphTypeMappingProvider(new ManualGraphTypeMappingProvider(typeMapping.ClrType, typeMapping.GraphType));
        }

        return builder;
    }
    #endregion

    #region - AddAutoClrMappings -
    /// <summary>
    /// Registers an instance of <see cref="AutoRegisteringGraphTypeMappingProvider"/> with the dependency injection
    /// framework as a singleton of type <see cref="IGraphTypeMappingProvider"/> and configures it to map input
    /// and/or output types to <see cref="AutoRegisteringInputObjectGraphType{TSourceType}"/> or
    /// <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> graph types.
    /// </summary>
    [RequiresUnreferencedCode("Please ensure that the CLR types used by your schema are not trimmed by the compiler.")]
    public static IGraphQLBuilder AddAutoClrMappings(this IGraphQLBuilder builder, bool mapInputTypes = true, bool mapOutputTypes = true)
    {
        builder.AddGraphTypeMappingProvider(new AutoRegisteringGraphTypeMappingProvider(mapInputTypes, mapOutputTypes));
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
    public static IGraphQLBuilder AddDocumentListener<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDocumentListener>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TDocumentListener : class, IDocumentExecutionListener
    {
        builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(serviceLifetime);
        builder.ConfigureExecutionOptions(options => options.Listeners.Add(options.RequestServicesOrThrow().GetRequiredService<TDocumentListener>()));
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

        builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(documentListener);
        builder.ConfigureExecutionOptions(options => options.Listeners.Add(documentListener));
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
        builder.Services.RegisterAsBoth<IDocumentExecutionListener, TDocumentListener>(documentListenerFactory ?? throw new ArgumentNullException(nameof(documentListenerFactory)), serviceLifetime);
        builder.ConfigureExecutionOptions(options => options.Listeners.Add(options.RequestServicesOrThrow().GetRequiredService<TDocumentListener>()));
        return builder;
    }
    #endregion

    #region - AddResolveFieldContextAccessor -
    /// <summary>
    /// Enables the <see cref="IResolveFieldContextAccessor"/> feature, which allows user code to retrieve
    /// the current <see cref="IResolveFieldContext"/> during field resolution. This is similar to
    /// <c>IHttpContextAccessor</c> in ASP.NET Core. Registers <see cref="IResolveFieldContextAccessor"/>
    /// as a singleton and configures the schema to populate the accessor during field execution.
    /// </summary>
    public static IGraphQLBuilder AddResolveFieldContextAccessor(this IGraphQLBuilder builder)
    {
        // Register the accessor as a singleton
        builder.Services.TryRegister<IResolveFieldContextAccessor>(ResolveFieldContextAccessor.Instance);

        // Configure the schema to use the accessor
        builder.ConfigureSchema<ConfigureResolveFieldContextAccessor>();

        return builder;
    }

    private sealed class ConfigureResolveFieldContextAccessor : IConfigureSchema
    {
        public void Configure(ISchema schema, IServiceProvider serviceProvider)
        {
            ((Schema)schema).ResolveFieldContextAccessor = serviceProvider.GetRequiredService<IResolveFieldContextAccessor>();
        }
    }
    #endregion

    #region - UseMiddleware -
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
    public static IGraphQLBuilder UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(this IGraphQLBuilder builder, bool install = true, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IFieldMiddleware
    {
        if (serviceLifetime == ServiceLifetime.Scoped)
        {
            // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
            throw new ArgumentOutOfRangeException(nameof(serviceLifetime), "Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
        }

        // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
        builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(serviceLifetime);
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
    public static IGraphQLBuilder UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(this IGraphQLBuilder builder, Func<IServiceProvider, ISchema, bool> installPredicate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IFieldMiddleware
    {
        if (installPredicate == null)
            throw new ArgumentNullException(nameof(installPredicate));

        if (serviceLifetime == ServiceLifetime.Scoped)
        {
            // this code prevents registrations of scoped middleware for a singleton schema, which is impossible.
            throw new ArgumentOutOfRangeException("Please specify a transient or singleton service lifetime. Specifying transient will cause the middleware lifetime to match that of the schema. Using a scoped schema will then have scoped middleware.");
        }

        // service lifetime defaults to transient so that the lifetime will match that of the schema, be it scoped or singleton
        builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(serviceLifetime);
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
    public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, bool install = true)
        where TMiddleware : class, IFieldMiddleware
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(middleware);
        if (install)
            builder.ConfigureSchema((schema, _) => schema.FieldMiddleware.Use(middleware));
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
    public static IGraphQLBuilder UseMiddleware<TMiddleware>(this IGraphQLBuilder builder, TMiddleware middleware, Func<IServiceProvider, ISchema, bool> installPredicate)
        where TMiddleware : class, IFieldMiddleware
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (installPredicate == null)
            throw new ArgumentNullException(nameof(installPredicate));

        builder.Services.RegisterAsBoth<IFieldMiddleware, TMiddleware>(middleware);
        builder.ConfigureSchema((schema, serviceProvider) =>
        {
            if (installPredicate(serviceProvider, schema))
                schema.FieldMiddleware.Use(middleware);
        });
        return builder;
    }
    #endregion

    #region - AddSerializer -
    /// <summary>
    /// Registers <typeparamref name="TSerializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
    /// dependency injection framework.
    /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
    /// </summary>
    public static IGraphQLBuilder AddSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSerializer>(this IGraphQLBuilder builder)
        where TSerializer : class, IGraphQLSerializer
    {
        builder.Services.Register<IGraphQLSerializer, TSerializer>(ServiceLifetime.Singleton, true);
        if (typeof(IGraphQLTextSerializer).IsAssignableFrom(typeof(TSerializer)))
            builder.Services.Register(typeof(IGraphQLTextSerializer), typeof(TSerializer), ServiceLifetime.Singleton, true);
        // builder.Services.Register(services => (IGraphQLTextSerializer)services.GetRequiredService<IGraphQLSerializer>(), ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers <paramref name="serializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
    /// dependency injection framework.
    /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
    /// </summary>
    public static IGraphQLBuilder AddSerializer<TSerializer>(this IGraphQLBuilder builder, TSerializer serializer)
        where TSerializer : class, IGraphQLSerializer
    {
        builder.Services.Register<IGraphQLSerializer>(serializer ?? throw new ArgumentNullException(nameof(serializer)), true);
        if (serializer is IGraphQLTextSerializer textSerializer)
            builder.Services.Register(textSerializer, true);
        return builder;
    }

    /// <summary>
    /// Registers <typeparamref name="TSerializer"/> as a singleton of type <see cref="IGraphQLSerializer"/> within the
    /// dependency injection framework. The supplied factory method is used to create the serializer.
    /// If supported, the class is also registered as type <see cref="IGraphQLTextSerializer"/>.
    /// </summary>
    public static IGraphQLBuilder AddSerializer<TSerializer>(this IGraphQLBuilder builder, Func<IServiceProvider, TSerializer> serializerFactory)
        where TSerializer : class, IGraphQLSerializer
    {
        builder.Services.Register<IGraphQLSerializer>(serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory)), ServiceLifetime.Singleton, true);
        if (typeof(IGraphQLTextSerializer).IsAssignableFrom(typeof(TSerializer)))
            builder.Services.Register(typeof(IGraphQLTextSerializer), serializerFactory, ServiceLifetime.Singleton, true);
        // builder.Services.Register(services => (IGraphQLTextSerializer)services.GetRequiredService<IGraphQLSerializer>(), ServiceLifetime.Singleton);
        return builder;
    }
    #endregion

    #region - ConfigureSchema and ConfigureExecutionOptions and ConfigureExecution -
    /// <summary>
    /// Configures an action to run prior to the code within the schema's constructor.
    /// Assumes that the schema derives from <see cref="Schema"/>.
    /// </summary>
    public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema> action)
        => action == null ? throw new ArgumentNullException(nameof(action)) : builder.ConfigureSchema((schema, _) => action(schema));

    /// <inheritdoc cref="ConfigureSchema(IGraphQLBuilder, Action{ISchema})"/>
    public static IGraphQLBuilder ConfigureSchema(this IGraphQLBuilder builder, Action<ISchema, IServiceProvider> action)
    {
        builder.Services.Register<IConfigureSchema>(new ConfigureSchema(action ?? throw new ArgumentNullException(nameof(action))));
        return builder;
    }

    /// <inheritdoc cref="ConfigureSchema(IGraphQLBuilder, Action{ISchema})"/>
    public static IGraphQLBuilder ConfigureSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConfigureSchema>(this IGraphQLBuilder builder)
        where TConfigureSchema : class, IConfigureSchema
    {
        builder.Services.TryRegister<IConfigureSchema, TConfigureSchema>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
        return builder;
    }

    /// <summary>
    /// Configures an action to configure execution options, which run prior to calls to
    /// <see cref="ConfigureExecution(IGraphQLBuilder, Func{ExecutionOptions, ExecutionDelegate, Task{ExecutionResult}})">ConfigureExecution</see>
    /// and other Use calls such as <see cref="UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>.
    /// <br/><br/>
    /// Assumes that the document executer is <see cref="DocumentExecuter"/>, or that it derives from <see cref="DocumentExecuter"/> and calls
    /// <see cref="DocumentExecuter(IDocumentBuilder, IDocumentValidator, IExecutionStrategySelector, IEnumerable{IConfigureExecution})"/>
    /// within the constructor.
    /// </summary>
    /// <remarks>
    /// <see cref="ExecutionOptions.RequestServices"/> can be used within the delegate to access the service provider for this execution.
    /// </remarks>
    public static IGraphQLBuilder ConfigureExecutionOptions(this IGraphQLBuilder builder, Action<ExecutionOptions> action)
        => ConfigureExecution(builder, new ConfigureExecutionOptions(action ?? throw new ArgumentNullException(nameof(action))));

    /// <inheritdoc cref="ConfigureExecutionOptions(IGraphQLBuilder, Action{ExecutionOptions})"/>
    public static IGraphQLBuilder ConfigureExecutionOptions(this IGraphQLBuilder builder, Func<ExecutionOptions, Task> action)
        => ConfigureExecution(builder, new ConfigureExecutionOptions(action ?? throw new ArgumentNullException(nameof(action))));

    /// <summary>
    /// Configures an action that can modify or replace document execution behavior, which runs after options configuration
    /// and immediately prior to document execution along with other calls to Use methods such as <see cref="UseApolloTracing(IGraphQLBuilder, bool)">UseApolloTracing</see>.
    /// </summary>
    /// <remarks>
    /// <see cref="ExecutionOptions.RequestServices"/> can be used within the delegate to access the service provider for this execution.
    /// </remarks>
    public static IGraphQLBuilder ConfigureExecution(this IGraphQLBuilder builder, Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> action)
        => ConfigureExecution(builder, new ConfigureExecution(action));

    /// <summary>
    /// Configures an action that can modify or replace document execution behavior.
    /// </summary>
    public static IGraphQLBuilder ConfigureExecution<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConfigureExecution>(this IGraphQLBuilder builder)
        where TConfigureExecution : class, IConfigureExecution
    {
        builder.Services.TryRegister<IConfigureExecution, TConfigureExecution>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
        return builder;
    }

    /// <inheritdoc cref="ConfigureExecution{TConfigureExecution}(IGraphQLBuilder)"/>
    public static IGraphQLBuilder ConfigureExecution<TConfigureExecution>(this IGraphQLBuilder builder, TConfigureExecution instance)
        where TConfigureExecution : class, IConfigureExecution
    {
        builder.Services.Register<IConfigureExecution>(instance);
        return builder;
    }

    /// <inheritdoc cref="ConfigureExecution{TConfigureExecution}(IGraphQLBuilder)"/>
    public static IGraphQLBuilder ConfigureExecution<TConfigureExecution>(this IGraphQLBuilder builder, Func<IServiceProvider, TConfigureExecution> factory)
        where TConfigureExecution : class, IConfigureExecution
    {
        builder.Services.Register<IConfigureExecution>(factory, ServiceLifetime.Singleton);
        return builder;
    }
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
    public static IGraphQLBuilder AddValidationRule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidationRule>(this IGraphQLBuilder builder, bool useForCachedDocuments = false, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TValidationRule : class, IValidationRule
    {
        builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(serviceLifetime);
        builder.ConfigureExecutionOptions(options =>
        {
            var rule = options.RequestServicesOrThrow().GetRequiredService<TValidationRule>();
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
    {
        builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(validationRule ?? throw new ArgumentNullException(nameof(validationRule)));
        builder.ConfigureExecutionOptions(options =>
        {
            options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(validationRule);
            if (useForCachedDocuments)
            {
                options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(validationRule);
            }
        });
        return builder;
    }

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
    public static IGraphQLBuilder AddValidationRule<TValidationRule>(this IGraphQLBuilder builder, Func<IServiceProvider, TValidationRule> validationRuleFactory, bool useForCachedDocuments = false, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TValidationRule : class, IValidationRule
    {
        builder.Services.RegisterAsBoth<IValidationRule, TValidationRule>(validationRuleFactory ?? throw new ArgumentNullException(nameof(validationRuleFactory)), serviceLifetime);
        builder.ConfigureExecutionOptions(options =>
        {
            var rule = options.RequestServicesOrThrow().GetRequiredService<TValidationRule>();
            options.ValidationRules = (options.ValidationRules ?? DocumentValidator.CoreRules).Append(rule);
            if (useForCachedDocuments)
            {
                options.CachedDocumentValidationRules = (options.CachedDocumentValidationRules ?? Enumerable.Empty<IValidationRule>()).Append(rule);
            }
        });
        return builder;
    }
    #endregion

    #region - UseApolloTracing -
    /// <summary>
    /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
    /// configures it to be installed within the schema, and configures responses to include Apollo
    /// Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
    /// When <paramref name="enableMetrics"/> is <see langword="true"/>, configures execution to set
    /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
    /// </summary>
    public static IGraphQLBuilder UseApolloTracing(this IGraphQLBuilder builder, bool enableMetrics = true)
        => UseApolloTracing(builder, _ => enableMetrics);

    /// <summary>
    /// Registers <see cref="InstrumentFieldsMiddleware"/> within the dependency injection framework and
    /// configures it to be installed within the schema, and configures responses to include Apollo
    /// Tracing data when enabled via <see cref="ExecutionOptions.EnableMetrics"/>.
    /// Configures execution to run <paramref name="enableMetricsPredicate"/> and when <see langword="true"/>, sets
    /// <see cref="ExecutionOptions.EnableMetrics"/> to <see langword="true"/>; otherwise leaves it unchanged.
    /// </summary>
    public static IGraphQLBuilder UseApolloTracing(this IGraphQLBuilder builder, Func<ExecutionOptions, bool> enableMetricsPredicate)
    {
        if (enableMetricsPredicate == null)
            throw new ArgumentNullException(nameof(enableMetricsPredicate));

        return builder
            .UseMiddleware<InstrumentFieldsMiddleware>()
            .ConfigureExecution(async (options, next) =>
            {
                if (enableMetricsPredicate(options))
                    options.EnableMetrics = true;
                var start = DateTime.UtcNow;
                var ret = await next(options).ConfigureAwait(false);
                if (options.EnableMetrics)
                {
                    ret.EnrichWithApolloTracing(start);
                }
                return ret;
            });
    }
    #endregion

    #region - AddExecutionStrategySelector -
    /// <summary>
    /// Registers <typeparamref name="TExecutionStrategySelector"/> with the dependency injection framework as
    /// a singleton of type <see cref="IExecutionStrategySelector"/>.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategySelector<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TExecutionStrategySelector>(this IGraphQLBuilder builder)
        where TExecutionStrategySelector : class, IExecutionStrategySelector
    {
        builder.Services.Register<IExecutionStrategySelector, TExecutionStrategySelector>(ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers <paramref name="executionStrategySelector"/> with the dependency injection framework as
    /// a singleton of type <see cref="IExecutionStrategySelector"/>.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategySelector<TExecutionStrategySelector>(this IGraphQLBuilder builder, TExecutionStrategySelector executionStrategySelector)
        where TExecutionStrategySelector : class, IExecutionStrategySelector
    {
        if (executionStrategySelector == null)
            throw new ArgumentNullException(nameof(executionStrategySelector));

        builder.Services.Register<IExecutionStrategySelector>(executionStrategySelector);
        return builder;
    }

    /// <summary>
    /// Registers <typeparamref name="TExecutionStrategySelector"/> with the dependency injection framework as
    /// a singleton of type <see cref="IExecutionStrategySelector"/>, using the supplied factory delegate.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategySelector<TExecutionStrategySelector>(this IGraphQLBuilder builder, Func<IServiceProvider, TExecutionStrategySelector> executionStrategySelectorFactory)
        where TExecutionStrategySelector : class, IExecutionStrategySelector
    {
        builder.Services.Register<IExecutionStrategySelector>(executionStrategySelectorFactory ?? throw new ArgumentNullException(nameof(executionStrategySelectorFactory)), ServiceLifetime.Singleton);
        return builder;
    }
    #endregion

    #region - AddExecutionStrategy -
    /// <summary>
    /// Registers <typeparamref name="TExecutionStrategy"/> with the dependency injection framework as
    /// a singleton, and registers an <see cref="ExecutionStrategyRegistration"/> for this <typeparamref name="TExecutionStrategy"/>
    /// configured for the selected <paramref name="operationType"/>.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TExecutionStrategy>(this IGraphQLBuilder builder, OperationType operationType)
        where TExecutionStrategy : class, IExecutionStrategy
    {
        builder.Services.Register<TExecutionStrategy>(ServiceLifetime.Singleton);
        builder.Services.Register(
            provider => new ExecutionStrategyRegistration(
                provider.GetRequiredService<TExecutionStrategy>(),
                operationType),
            ServiceLifetime.Singleton);

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="ExecutionStrategyRegistration"/> with the dependency injection framework
    /// for the specified <paramref name="executionStrategy"/> and <paramref name="operationType"/>.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategy<TExecutionStrategy>(this IGraphQLBuilder builder, TExecutionStrategy executionStrategy, OperationType operationType)
        where TExecutionStrategy : class, IExecutionStrategy
    {
        if (executionStrategy == null)
            throw new ArgumentNullException(nameof(executionStrategy));

        builder.Services.Register(
            new ExecutionStrategyRegistration(
                executionStrategy,
                operationType));

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="ExecutionStrategyRegistration"/> with the dependency injection framework
    /// as a singleton for the specified <typeparamref name="TExecutionStrategy"/> and <paramref name="operationType"/>,
    /// using the supplied factory delegate.
    /// </summary>
    public static IGraphQLBuilder AddExecutionStrategy<TExecutionStrategy>(this IGraphQLBuilder builder, Func<IServiceProvider, TExecutionStrategy> executionStrategyFactory, OperationType operationType)
        where TExecutionStrategy : class, IExecutionStrategy
    {
        if (executionStrategyFactory == null)
            throw new ArgumentNullException(nameof(executionStrategyFactory));

        builder.Services.Register(
            provider => new ExecutionStrategyRegistration(
                executionStrategyFactory(provider),
                operationType),
            ServiceLifetime.Singleton);

        return builder;
    }
    #endregion

    #region - UseTelemetry -
#if NET5_0_OR_GREATER
    /// <summary>
    /// Configures the GraphQL engine to collect traces via the <see cref="System.Diagnostics.Activity">System.Diagnostics.Activity API</see> and records events that match the
    /// <see href="https://opentelemetry.io/docs/specs/semconv/database/graphql/">OpenTelemetry recommendations</see>.
    /// Trace data contains the GraphQL operation name, the operation type, and the optionally the document.
    /// Disables auto-instrumentation for GraphQL.
    /// </summary>
    /// <remarks>
    /// When applicable, place after calls to UseAutomaticPersistedQueries to ensure that the query document is recorded properly.
    /// <br/>
    /// To instruct OpenTelemetry SDK to collect the traces produced by GraphQL.NET register the
    /// '<see cref="GraphQLTelemetryProvider.SourceName"/>' source name with the TracerProviderBuilder.
    /// <code>
    /// services
    ///     .AddOpenTelemetry()
    ///     .WithTracing(tracing =&gt; tracing
    ///         .AddSource(GraphQLTelemetryProvider.SourceName));
    /// </code>
    /// </remarks>
    public static IGraphQLBuilder UseTelemetry(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions>? configure = null)
        => UseTelemetry<GraphQLTelemetryProvider>(builder, configure);

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions, IServiceProvider>? configure)
        => UseTelemetry<GraphQLTelemetryProvider>(builder, configure);

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions>? configure = null)
        where TProvider : GraphQLTelemetryProvider
        => UseTelemetry<TProvider, GraphQLTelemetryOptions>(builder, configure);

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(this IGraphQLBuilder builder, Action<GraphQLTelemetryOptions, IServiceProvider>? configure)
        where TProvider : GraphQLTelemetryProvider
        => UseTelemetry<TProvider, GraphQLTelemetryOptions>(builder, configure);

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider, TOptions>(this IGraphQLBuilder builder, Action<TOptions>? configure = null)
        where TProvider : GraphQLTelemetryProvider
        where TOptions : GraphQLTelemetryOptions, new()
        => UseTelemetry<TProvider, TOptions>(builder, configure != null ? (opts, _) => configure(opts) : null);

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider, TOptions>(this IGraphQLBuilder builder, Action<TOptions, IServiceProvider>? configure)
        where TProvider : GraphQLTelemetryProvider
        where TOptions : GraphQLTelemetryOptions, new()
    {
        OpenTelemetry.AutoInstrumentation.Initializer.Enabled = false;
        builder.Services.Configure(configure);
        builder.Services.TryRegister<IConfigureExecution, TProvider>(ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);
        return builder;
    }

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<TProvider>(this IGraphQLBuilder builder, TProvider telemetryProvider)
        where TProvider : GraphQLTelemetryProvider
    {
        if (telemetryProvider == null)
            throw new ArgumentNullException(nameof(telemetryProvider));
        OpenTelemetry.AutoInstrumentation.Initializer.Enabled = false;
        builder.ConfigureExecution(telemetryProvider);
        return builder;
    }

    /// <inheritdoc cref="UseTelemetry(IGraphQLBuilder, Action{GraphQLTelemetryOptions}?)"/>
    public static IGraphQLBuilder UseTelemetry<TProvider>(this IGraphQLBuilder builder, Func<IServiceProvider, TProvider> telemetryProviderFactory)
        where TProvider : GraphQLTelemetryProvider
    {
        if (telemetryProviderFactory == null)
            throw new ArgumentNullException(nameof(telemetryProviderFactory));
        OpenTelemetry.AutoInstrumentation.Initializer.Enabled = false;
        builder.ConfigureExecution(telemetryProviderFactory);
        return builder;
    }
#endif
    #endregion

    #region - ConfigureUnhandledExceptionHandler -
    /// <summary>
    /// Configures the delegate to be called when an unhandled exception occurs during document execution.
    /// This is typically used to log exceptions to a database for further review.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service provider can be accessed via <see cref="IExecutionContext.RequestServices"/> or <see cref="ExecutionOptions.RequestServices"/>.
    /// </para>
    /// <para>
    /// With APQ support, <see cref="ExecutionOptions.Query"/> may be <see langword="null"/>.
    /// To retrieve the executed query as a string value use the following:
    /// <code>
    /// var query = options.Query ?? options.Document?.Source.ToString();
    /// </code>
    /// </para>
    /// </remarks>
    public static IGraphQLBuilder AddUnhandledExceptionHandler(this IGraphQLBuilder builder, Func<UnhandledExceptionContext, Task> unhandledExceptionDelegate)
    {
        if (unhandledExceptionDelegate == null)
            throw new ArgumentNullException(nameof(unhandledExceptionDelegate));

        return builder.ConfigureExecutionOptions(settings => settings.UnhandledExceptionDelegate = unhandledExceptionDelegate);
    }

    /// <inheritdoc cref="AddUnhandledExceptionHandler(IGraphQLBuilder, Func{UnhandledExceptionContext, Task})"/>
    public static IGraphQLBuilder AddUnhandledExceptionHandler(this IGraphQLBuilder builder, Func<UnhandledExceptionContext, ExecutionOptions, Task> unhandledExceptionDelegate)
    {
        if (unhandledExceptionDelegate == null)
            throw new ArgumentNullException(nameof(unhandledExceptionDelegate));

        return builder.ConfigureExecutionOptions(settings => settings.UnhandledExceptionDelegate = context => unhandledExceptionDelegate(context, settings));
    }

    /// <inheritdoc cref="AddUnhandledExceptionHandler(IGraphQLBuilder, Func{UnhandledExceptionContext, Task})"/>
    public static IGraphQLBuilder AddUnhandledExceptionHandler(this IGraphQLBuilder builder, Action<UnhandledExceptionContext> unhandledExceptionDelegate)
    {
        if (unhandledExceptionDelegate == null)
            throw new ArgumentNullException(nameof(unhandledExceptionDelegate));

        var handler = (UnhandledExceptionContext context) =>
        {
            unhandledExceptionDelegate(context);
            return Task.CompletedTask;
        };
        builder.ConfigureExecutionOptions(settings => settings.UnhandledExceptionDelegate = handler);
        return builder;
    }

    /// <inheritdoc cref="AddUnhandledExceptionHandler(IGraphQLBuilder, Func{UnhandledExceptionContext, Task})"/>
    [Obsolete("Reference the UnhandledExceptionContext.ExecutionOptions property instead of using this overload. This method will be removed in v9.")]
    public static IGraphQLBuilder AddUnhandledExceptionHandler(this IGraphQLBuilder builder, Action<UnhandledExceptionContext, ExecutionOptions> unhandledExceptionDelegate)
    {
        if (unhandledExceptionDelegate == null)
            throw new ArgumentNullException(nameof(unhandledExceptionDelegate));

        var handler = (UnhandledExceptionContext context) =>
        {
            unhandledExceptionDelegate(context, context.ExecutionOptions);
            return Task.CompletedTask;
        };
        builder.ConfigureExecutionOptions(settings => settings.UnhandledExceptionDelegate = handler);
        return builder;
    }
    #endregion

    #region - AddSchemaVisitor -
    /// <summary>
    /// Registers <typeparamref name="TSchemaVisitor"/> with the dependency injection framework as
    /// a singleton and registers <typeparamref name="TSchemaVisitor"/> within the schema configuration.
    /// </summary>
    public static IGraphQLBuilder AddSchemaVisitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSchemaVisitor>(this IGraphQLBuilder builder)
        where TSchemaVisitor : class, ISchemaNodeVisitor
    {
        builder.Services.Register<TSchemaVisitor>(ServiceLifetime.Singleton);
        builder.ConfigureSchema(schema => schema.RegisterVisitor<TSchemaVisitor>());
        return builder;
    }

    /// <summary>
    /// Registers <paramref name="schemaVisitor"/> within the schema configuration.
    /// </summary>
    public static IGraphQLBuilder AddSchemaVisitor<TSchemaVisitor>(this IGraphQLBuilder builder, TSchemaVisitor schemaVisitor)
        where TSchemaVisitor : class, ISchemaNodeVisitor
    {
        if (schemaVisitor == null)
            throw new ArgumentNullException(nameof(schemaVisitor));
        builder.ConfigureSchema(schema => schema.RegisterVisitor(schemaVisitor));
        return builder;
    }

    /// <summary>
    /// Registers <typeparamref name="TSchemaVisitor"/> within the dependency injection framework as a singleton
    /// using the supplied factory delegate and registers <typeparamref name="TSchemaVisitor"/> within the schema configuration.
    /// </summary>
    public static IGraphQLBuilder AddSchemaVisitor<TSchemaVisitor>(this IGraphQLBuilder builder, Func<IServiceProvider, TSchemaVisitor> schemaVisitorFactory)
        where TSchemaVisitor : class, ISchemaNodeVisitor
    {
        if (schemaVisitorFactory == null)
            throw new ArgumentNullException(nameof(schemaVisitorFactory));
        builder.Services.Register(schemaVisitorFactory, ServiceLifetime.Singleton);
        builder.ConfigureSchema(schema => schema.RegisterVisitor<TSchemaVisitor>());
        return builder;
    }
    #endregion

    #region - UsePersistedDocuments -
    /// <summary>
    /// Adds support of Persisted Documents, a draft appendix to the draft GraphQL over HTTP specification; see
    /// <see href="https://github.com/graphql/graphql-over-http/pull/264"/>. The specified implementation of
    /// <see cref="IPersistedDocumentLoader"/> is used to retrieve query strings from supplied document identifiers.
    /// By default, arbitrary queries will be disabled; configure <see cref="PersistedDocumentOptions.AllowOnlyPersistedDocuments"/>
    /// if desired.
    /// </summary>
    public static IGraphQLBuilder UsePersistedDocuments<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoader>(this IGraphQLBuilder builder, DI.ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, Action<PersistedDocumentOptions>? action = null)
        where TLoader : class, IPersistedDocumentLoader
        => builder.UsePersistedDocuments<TLoader>(serviceLifetime, action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="UsePersistedDocuments{TLoader}(IGraphQLBuilder, ServiceLifetime, Action{PersistedDocumentOptions}?)"/>
    public static IGraphQLBuilder UsePersistedDocuments<TLoader>(this IGraphQLBuilder builder, DI.ServiceLifetime serviceLifetime, Action<PersistedDocumentOptions, IServiceProvider>? action)
        where TLoader : class, IPersistedDocumentLoader
    {
        builder.Services.Register<IPersistedDocumentLoader, TLoader>(serviceLifetime);
        builder.Services.Configure(action);
        return builder.ConfigureExecution<PersistedDocumentHandler>();
    }

    /// <summary>
    /// Adds support of Persisted Documents, a draft appendix to the draft GraphQL over HTTP specification; see
    /// <see href="https://github.com/graphql/graphql-over-http/pull/264"/>. Requires the
    /// <see cref="PersistedDocumentOptions.GetQueryDelegate"/> to be set to a delegate that can retrieve the
    /// query string from the document identifier. By default, arbitrary queries will be disabled; configure
    /// <see cref="PersistedDocumentOptions.AllowOnlyPersistedDocuments"/> if desired.
    /// </summary>
    public static IGraphQLBuilder UsePersistedDocuments(this IGraphQLBuilder builder, Action<PersistedDocumentOptions>? action)
        => builder.UsePersistedDocuments(action == null ? null : (options, _) => action(options));

    /// <inheritdoc cref="UsePersistedDocuments(IGraphQLBuilder, Action{PersistedDocumentOptions})"/>
    public static IGraphQLBuilder UsePersistedDocuments(this IGraphQLBuilder builder, Action<PersistedDocumentOptions, IServiceProvider>? action)
    {
        builder.Services.Configure(action);
        return builder.ConfigureExecution<PersistedDocumentHandler>();
    }
    #endregion

    #region - WithTimeout -
    /// <summary>
    /// Configures a timeout for the execution of a GraphQL request. If the timeout is exceeded, a timeout error
    /// formatted as a GraphQL response will be returned.
    /// </summary>
    public static IGraphQLBuilder WithTimeout(this IGraphQLBuilder builder, TimeSpan timeout)
        => WithTimeout(builder, timeout, TimeoutAction.ReturnTimeoutError);

    /// <summary>
    /// Configures a timeout for the execution of a GraphQL request. If the timeout is exceeded, the specified
    /// <paramref name="timeoutDelegate"/> will be invoked to generate a response.
    /// </summary>
    public static IGraphQLBuilder WithTimeout(this IGraphQLBuilder builder, TimeSpan timeout, Func<ExecutionOptions, ExecutionResult> timeoutDelegate)
        => WithTimeout(builder, timeout, options => Task.FromResult(timeoutDelegate(options)));

    /// <inheritdoc cref="WithTimeout(IGraphQLBuilder, TimeSpan, Func{ExecutionOptions, ExecutionResult})"/>
    public static IGraphQLBuilder WithTimeout(this IGraphQLBuilder builder, TimeSpan timeout, Func<ExecutionOptions, Task<ExecutionResult>> timeoutDelegate)
    {
        builder.WithTimeout(timeout, TimeoutAction.ThrowTimeoutException);
        builder.ConfigureExecution(async (options, next) =>
        {
            try
            {
                return await next(options).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return await timeoutDelegate(options).ConfigureAwait(false);
            }
        });
        return builder;
    }

    /// <summary>
    /// Configures a timeout for the execution of a GraphQL request. If the timeout is exceeded, the specified
    /// <paramref name="timeoutAction"/> will be taken.
    /// </summary>
    public static IGraphQLBuilder WithTimeout(this IGraphQLBuilder builder, TimeSpan timeout, TimeoutAction timeoutAction)
    {
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        builder.ConfigureExecutionOptions(options =>
        {
            options.Timeout = timeout;
            options.TimeoutAction = timeoutAction;
        });
        return builder;
    }
    #endregion
}
