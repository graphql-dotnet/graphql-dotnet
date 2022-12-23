using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AotSampleApp;

internal interface IAotServiceProviderConfiguration
{
    IAotServiceProviderConfiguration AddListType<T>();
}

internal class AotServiceProvider : IServiceProvider, IDisposable, IServiceScopeFactory, IServiceScope, IAotServiceProviderConfiguration
{
    private readonly ILookup<Type, MutableDescriptor> _services;
    private readonly AotServiceProvider? _rootServiceProvider;
    private readonly List<IDisposable> _disposables = new();
    private readonly IEnumerable<MutableDescriptor> _descriptors;

    private AotServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors)
    {
        _rootServiceProvider = null;
        if (serviceDescriptors.Any(x => x.ServiceType.IsGenericTypeDefinition && x.ImplementationType == null))
            throw new InvalidOperationException("Generic services must provide an implementation type; not a factory or instance.");
        _descriptors = serviceDescriptors.Select(x => new MutableDescriptor(x)).ToList();
        _services = _descriptors.ToLookup(x => x.ServiceType);
        foreach (var desc in _descriptors.Where(x => x.ImplementationType != null && x.ImplementationFactory == null && !x.ImplementationType.IsGenericTypeDefinition))
        {
            desc.ImplementationFactory = CreateInstanceFunc(this, desc.ImplementationType!);
        }
    }

    private AotServiceProvider(IEnumerable<MutableDescriptor> serviceDescriptors, AotServiceProvider rootServiceProvider)
    {
        _rootServiceProvider = rootServiceProvider;
        if (serviceDescriptors.Any(x => x.ServiceType.IsGenericTypeDefinition && x.ImplementationType == null))
            throw new InvalidOperationException("Generic services must provide an implementation type; not a factory or instance.");
        _descriptors = serviceDescriptors.Select(x => new MutableDescriptor(x)).ToList();
        _services = serviceDescriptors.ToLookup(x => x.ServiceType);
    }

    public static AotServiceProvider Create(IServiceCollection services, Action<IAotServiceProviderConfiguration>? configuration = null)
    {
        var ret = new AotServiceProvider(services);
        if (configuration != null)
            configuration(ret);
        return ret;
    }

    public IAotServiceProviderConfiguration AddListType<T>()
    {
        Preserve<T[]>();
        return this;
    }

    private static void Preserve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
    }

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => new AotServiceProvider(_descriptors, _rootServiceProvider ?? this);

    public void Dispose()
    {
        foreach (var obj in _disposables)
        {
            obj.Dispose();
        }
    }

    public bool CanGetService(Type serviceType)
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

    public object? GetService(Type serviceType)
    {
        // return known objects
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
            return this;

        // attempt to match on the last registration of the specified type, even an IEnumerable
        var serviceDescriptor = _services[serviceType].LastOrDefault();
        if (serviceDescriptor != null)
            return Construct(serviceType, serviceDescriptor);

        // for IEnumerable<T>, create a list of instances
        if (serviceType.IsGenericType)
        {
            var genericType = serviceType.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>))
            {
                var nestedServiceType = serviceType.GetGenericArguments()[0];
                var serviceDescriptors = _services[nestedServiceType];
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                IList list = Array.CreateInstance(nestedServiceType, serviceDescriptors.Count());
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                int i = 0;
                foreach (var row in serviceDescriptors)
                    list[i] = Construct(nestedServiceType, row);
                return list;
            }

            var genericDescriptor = _services[genericType].LastOrDefault();
            if (genericDescriptor != null)
            {
                if (genericDescriptor.ServiceType == genericDescriptor.ImplementationType)
                    return CreateInstance(genericDescriptor.ImplementationType);
                var genericArgs = serviceType.GetGenericArguments();
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable IL2055
                var implementationType = genericDescriptor.ImplementationType!.MakeGenericType(genericArgs);
#pragma warning restore IL2055
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                //throw new ApplicationException("huh");
                return CreateInstance(implementationType);
            }
        }

        // no match found
        return null;

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
    }

    private object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        //return ActivatorUtilities.CreateInstance(this, implementationType);
        return CreateInstanceFunc(this, implementationType)(this);
    }

    private static readonly Expression<Func<IServiceProvider, Type, object?>> _getServiceExpr = (sp, t) => sp.GetService(t);
    internal static Func<IServiceProvider, object> CreateInstanceFunc(AotServiceProvider provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        var getServiceMethod = ((MethodCallExpression)_getServiceExpr.Body).Method;
        var ctor = SelectConstructor(provider, implementationType.GetConstructors());
        if (ctor == null)
            throw new InvalidOperationException($"Cannot construct type {implementationType} because it has no constructors.");
        var param = Expression.Parameter(typeof(IServiceProvider));
        var body = Expression.New(
            ctor,
            ctor.GetParameters().Select(parameter => Expression.Convert(Expression.Call(param, getServiceMethod, Expression.Constant(parameter.ParameterType)), parameter.ParameterType))
        );
        var lambda = Expression.Lambda<Func<IServiceProvider, object>>(body, param);
        return lambda.Compile();
    }

    private static ConstructorInfo? SelectConstructor(AotServiceProvider provider, ConstructorInfo[] constructors)
    {
        return constructors
            .OrderByDescending(x => x.GetParameters().Length)
            .Where(x => x.GetParameters().Select(x => x.ParameterType).All(t => provider.CanGetService(t)))
            .FirstOrDefault();
    }

    private class MutableDescriptor
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
