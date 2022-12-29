using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.AotCompilationSample;

/// <summary>
/// Provides configuration methods for the <see cref="AotServiceProvider"/>.
/// </summary>
internal interface IAotServiceProviderConfiguration
{
    /// <summary>
    /// Ensures that the specified type can be constructed from the service provider.
    /// Useful for generic types.
    /// </summary>
    IAotServiceProviderConfiguration AddType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>();
}

internal static class AotServiceProviderConfigurationExtensions
{
    /// <summary>
    /// Ensures that <see cref="IEnumerable{T}"/> can be fulfilled from the service provider.
    /// </summary>
    public static IAotServiceProviderConfiguration AddEnumerableType<T>(this IAotServiceProviderConfiguration builder)
        => builder.AddType<T[]>();
}

/// <summary>
/// A service provider that is compatible with ahead-of-time (AOT) compilation while retaining all standard features.
/// </summary>
internal sealed class AotServiceProvider : IServiceProvider, IDisposable, IServiceScopeFactory, IServiceScope, IAotServiceProviderConfiguration, IAsyncDisposable
{
    private Dictionary<Type, object>? _serviceImplementations;
    private readonly ILookup<Type, MutableDescriptor> _services;
    private readonly AotServiceProvider? _rootServiceProvider;
    private readonly List<IDisposable> _disposables = new();
    private readonly IEnumerable<MutableDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="AotServiceProvider"/> class as a root service scope.
    /// </summary>
    private AotServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors)
    {
        if (serviceDescriptors == null)
            throw new ArgumentNullException(nameof(serviceDescriptors));

        // double check that the service descriptors are valid
        if (serviceDescriptors.Any(x => x.ServiceType.IsGenericTypeDefinition && x.ImplementationType == null))
            throw new InvalidOperationException("Generic services must provide an implementation type; not a factory or instance.");

        // duplicate the descriptors so that we can modify them
        _descriptors = serviceDescriptors.Select(x => new MutableDescriptor(x)).ToList();
        _services = _descriptors.ToLookup(x => x.ServiceType);

        // pre-compile factories for non-generic services that do not have a factory or instance assigned
        foreach (var desc in _descriptors.Where(x => x.ImplementationType != null && !x.ImplementationType.IsGenericTypeDefinition))
        {
            desc.ImplementationFactory = CreateImplementationFactory(this, desc.ImplementationType!, true);
        }
    }

    /// <summary>
    /// Initializes a new service scope.
    /// </summary>
    private AotServiceProvider(AotServiceProvider rootServiceProvider)
    {
        _rootServiceProvider = rootServiceProvider;
        _descriptors = rootServiceProvider._descriptors.Select(x => new MutableDescriptor(x)).ToList();
        _services = _descriptors.ToLookup(x => x.ServiceType);
    }

    /// <summary>
    /// Creates a new <see cref="AotServiceProvider"/> from the specified <see cref="ServiceDescriptor"/>s.
    /// </summary>
    public static AotServiceProvider Create(IServiceCollection services, Action<IAotServiceProviderConfiguration>? configuration = null)
    {
        var ret = new AotServiceProvider(services);
        configuration?.Invoke(ret);
        return ret;
    }

    IAotServiceProviderConfiguration IAotServiceProviderConfiguration.AddType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
        GC.KeepAlive(typeof(T)); // dunno why this line is required, but it is -- without it, sometimes the constructors are not preserved
        return this;
    }

    IServiceProvider IServiceScope.ServiceProvider => this;

    IServiceScope IServiceScopeFactory.CreateScope()
        => new AotServiceProvider(_rootServiceProvider ?? this);

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var obj in _disposables)
        {
            obj.Dispose();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var obj in _disposables)
        {
            if (obj is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                obj.Dispose();
        }
    }

    private bool CanGetService(Type serviceType)
    {
        // known objects
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
            return true;

        // attempt to match on the last registration of the specified type, even an IEnumerable
        if (_services[serviceType].Any())
            return true;

        // for IEnumerable<T>, create a list of instances
        if (serviceType.IsGenericType)
        {
            var genericType = serviceType.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>))
                return true;

            if (_services[genericType].Any())
                return true;
        }

        // no match found
        return false;
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        // return known objects
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
            return this;

        // attempt to match on generic implementations which have already been created
        if (_serviceImplementations?.TryGetValue(serviceType, out var implementation) ?? false)
        {
            return implementation;
        }

        // attempt to match on the last registration of the specified type -- even an IEnumerable or array type
        var serviceDescriptor = _services[serviceType].LastOrDefault();
        if (serviceDescriptor != null)
            return Construct(serviceType, serviceDescriptor);

        // for T[], return a list of instances
        if (serviceType.IsArray)
        {
            var elementType = serviceType.GetElementType()!;
            return ConstructEnumerable(elementType);
        }

        if (serviceType.IsGenericType)
        {
            // for IEnumerable<T>, return a list of instances
            var genericType = serviceType.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>))
            {
                var nestedServiceType = serviceType.GetGenericArguments()[0];
                return ConstructEnumerable(nestedServiceType);
            }

            // for generic types, attempt to match on the last registration of the generic type
            var genericDescriptor = _services[genericType].LastOrDefault();
            if (genericDescriptor != null)
            {
                Type implementationType = serviceType;
                object instance;
                if (genericDescriptor.ServiceType != genericDescriptor.ImplementationType)
                {
                    var genericArgs = serviceType.GetGenericArguments();
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
                    implementationType = genericDescriptor.ImplementationType!.MakeGenericType(genericArgs);
#pragma warning restore IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                }
#pragma warning disable IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
                instance = CreateInstance(implementationType);
#pragma warning restore IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.

                // if scoped/singleton, use this instance going forwards
                if (genericDescriptor.Lifetime != ServiceLifetime.Transient)
                    (_serviceImplementations ??= new()).Add(implementationType, instance);

                // register the disposable if applicable
                if (instance is IDisposable disposable)
                    _disposables.Add(disposable);

                // return the instance
                return instance;
            }
        }

        // no match found
        return null;

        // returns a list of instances for the specified service type
        // note: may require that AddEnumerableType was called during configuration
        object ConstructEnumerable(Type type)
        {
            var serviceDescriptors = _services[type];
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            IList list = Array.CreateInstance(type, serviceDescriptors.Count());
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            int i = 0;
            foreach (var descriptor in serviceDescriptors)
                list[i] = Construct(type, descriptor);
            return list;
        }

        // constructs or returns an instance of the specified type, registering it for disposal if necessary
        object Construct(Type serviceType, MutableDescriptor descriptor)
        {
            // don't register a disposable if it (a) comes from the parent service provider (b) was registered with an implementation (c) already been instantiated
            if (descriptor.Lifetime == ServiceLifetime.Singleton && _rootServiceProvider != null)
                return _rootServiceProvider.GetService(serviceType)!;
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            // use the factory to create the instance
            object instance;
            if (descriptor.ImplementationFactory != null)
                instance = descriptor.ImplementationFactory(this);
            else if (descriptor.ImplementationType != null)
                instance = CreateInstance(descriptor.ImplementationType);
            else
                throw new InvalidOperationException("Service descriptor does not contain any implementation.");

            // if scoped/singleton, use this instance going forwards
            if (descriptor.Lifetime != ServiceLifetime.Transient)
                descriptor.ImplementationInstance = instance;

            // register the disposable if applicable
            if (instance is IDisposable disposable)
                _disposables.Add(disposable);

            // return the instance
            return instance;
        }

        // creates an instance of the specified type (typically used for generic types)
        // note: don't compile as this is really only used for generic types, and since we are not
        //   caching the factory, it will need to be recreated when requested in the future
        // note: may require that the specific generic type implementation is statically referenced
        //   or else it may have been trimmed out
        object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
            => CreateImplementationFactory(this, implementationType, false)(this);
    }

    /// <summary>
    /// A reference to <see cref="IServiceProvider.GetService(Type)"/>.
    /// </summary>
    private static readonly MethodInfo _getServiceMethod;
    static AotServiceProvider()
    {
        Expression<Func<IServiceProvider, Type, object?>> getServiceExpr = (sp, t) => sp.GetService(t);
        _getServiceMethod = ((MethodCallExpression)getServiceExpr.Body).Method;
    }

    /// <summary>
    /// Creates a implementation factory method for a given type.
    /// </summary>
    private static Func<IServiceProvider, object> CreateImplementationFactory(AotServiceProvider provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, bool compile)
    {
        // find the best constructor to use
        var ctor = SelectConstructor(provider, implementationType.GetConstructors());
        if (ctor == null)
            throw new InvalidOperationException($"Cannot construct type '{implementationType}' because it has no constructors with parameters that can be fulfilled.");

        // create a lambda to create an implementation given a service provider
        var param = Expression.Parameter(typeof(IServiceProvider));
        var body = Expression.New(
            ctor,
            ctor.GetParameters().Select(parameter => Expression.Convert(Expression.Call(param, _getServiceMethod, Expression.Constant(parameter.ParameterType)), parameter.ParameterType))
        );
        var lambda = Expression.Lambda<Func<IServiceProvider, object>>(body, param);

        // for AOT-compiled scenarios, or when compile == false, the "compiled" lambda is interpreted at runtime, not actually compiled.
        // for non-AOT scenarios and when compile == true, the "compiled" lambda is compiled to IL and cached.
        return lambda.Compile(!compile);

        static ConstructorInfo? SelectConstructor(AotServiceProvider provider, ConstructorInfo[] constructors)
        {
            // find the constructor with the most parameters that can be fulfilled
            // if two constructors that have the same number of parameters can both be fulfilled, select the first one
            return constructors
                .OrderByDescending(ctor => ctor.GetParameters().Length)
                .Where(ctor => ctor.GetParameters().Select(param => param.ParameterType).All(type => provider.CanGetService(type)))
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// A mutable service descriptor, so that <see cref="ImplementationFactory"/> can be initialized
    /// when the service provider is created, and so that <see cref="ImplementationInstance"/> can be
    /// set when a non-transient service is requested.
    /// </summary>
    private sealed class MutableDescriptor
    {
        public MutableDescriptor(ServiceDescriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            Lifetime = descriptor.Lifetime;
            ImplementationType = descriptor.ImplementationType;
            ImplementationFactory = descriptor.ImplementationFactory;
            ImplementationInstance = descriptor.ImplementationInstance;
            OriginalImplementationInstance = descriptor.ImplementationInstance;
        }

        public MutableDescriptor(MutableDescriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            Lifetime = descriptor.Lifetime;
            ImplementationType = descriptor.ImplementationType;
            ImplementationFactory = descriptor.ImplementationFactory;
            ImplementationInstance = descriptor.OriginalImplementationInstance;
            OriginalImplementationInstance = descriptor.OriginalImplementationInstance;
        }

        public Type ServiceType { get; }
        public ServiceLifetime Lifetime { get; }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? ImplementationType { get; }
        public Func<IServiceProvider, object>? ImplementationFactory { get; set; }
        public object? ImplementationInstance { get; set; }
        public object? OriginalImplementationInstance { get; }
    }
}
