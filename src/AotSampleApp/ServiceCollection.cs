using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace AotSampleApp;

internal interface IAotServiceProviderConfiguration
{
    IAotServiceProviderConfiguration AddListType<T>();
}

internal class AotServiceProvider : IServiceProvider, IDisposable, IServiceScopeFactory, IServiceScope, IAotServiceProviderConfiguration
{
    private readonly ILookup<Type, MutableDescriptor> _services;
    private readonly IServiceProvider? _parentServiceProvider;
    private readonly List<IDisposable> _disposables = new();
    private readonly IEnumerable<ServiceDescriptor> _originalDescriptors;
    private readonly Dictionary<Type, Type> _listTypes;

    private AotServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProvider? parentServiceProvider, Dictionary<Type, Type> listTypes)
    {
        _listTypes = listTypes;
        _parentServiceProvider = parentServiceProvider;
        _originalDescriptors = serviceDescriptors;
        _services = serviceDescriptors
            .Where(x => !x.ServiceType.ContainsGenericParameters) // open generics are not supported
            .ToLookup(x => x.ServiceType, x => new MutableDescriptor(x));
    }

    public static AotServiceProvider Create(IServiceCollection services, Action<IAotServiceProviderConfiguration> configuration)
    {
        var ret = new AotServiceProvider(services, null, new Dictionary<Type, Type>());
        configuration(ret);
        return ret;
    }

    public IAotServiceProviderConfiguration AddListType<T>()
    {
        _listTypes.Add(typeof(T), typeof(List<T>));
        return this;
    }

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => new AotServiceProvider(_originalDescriptors, this, _listTypes);

    public void Dispose()
    {
        foreach (var obj in _disposables)
        {
            obj.Dispose();
        }
    }

    public object? GetService(Type serviceType)
    {
        // return known objects
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
            return this;

        // pull all matching service descriptors for the specified service type
        var serviceDescriptors = _services[serviceType];

        // attempt to match on the last registration of the specified type, even an IEnumerable
        var serviceDescriptor = serviceDescriptors.LastOrDefault();
        if (serviceDescriptor != null)
            return Construct(serviceType, serviceDescriptor);

        // for IEnumerable<T>, create a list of instances
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var nestedServiceType = serviceType.GetGenericArguments()[0];
            if (!_listTypes.TryGetValue(nestedServiceType, out var listFactory))
                throw new InvalidOperationException($"No list type configured for type {nestedServiceType}");
            var list = (IList)Activator.CreateInstance(listFactory)!;
            foreach (var row in serviceDescriptors)
                list.Add(Construct(nestedServiceType, row));
            return list;
        }

        // no match found
        return null;

        object Construct(Type serviceType, MutableDescriptor descriptor)
        {
            // don't register a disposable if it (a) comes from the parent service provider (b) was registered with an implementation (c) already been instantiated
            if (descriptor.Lifetime == ServiceLifetime.Singleton && _parentServiceProvider != null)
                return _parentServiceProvider.GetService(serviceType)!;
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            // use the factory to create the instance
            object instance;
            if (descriptor.ImplementationFactory != null)
                instance = descriptor.ImplementationFactory(this);
            else if (descriptor.ImplementationType != null)
                instance = ActivatorUtilities.CreateInstance(this, descriptor.ImplementationType);
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

    private class MutableDescriptor
    {
        public MutableDescriptor(ServiceDescriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            Lifetime = descriptor.Lifetime;
            ImplementationType = descriptor.ImplementationType;
            ImplementationFactory = descriptor.ImplementationFactory;
            ImplementationInstance = descriptor.ImplementationInstance;
        }

        public Type ServiceType { get; }
        public ServiceLifetime Lifetime { get; }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? ImplementationType { get; }
        public Func<IServiceProvider, object>? ImplementationFactory { get; }
        public object? ImplementationInstance { get; set; }
    }
}
