using GraphQL.DI;
using StructureMap;
using StructureMap.Pipeline;

namespace GraphQL.StructureMap;

public class GraphQLBuilder : GraphQLBuilderBase, IServiceRegister
{
    /// <inheritdoc />
    public override IServiceRegister Services => this;

    public IRegistry Registry { get; }

    public GraphQLBuilder(IRegistry registry, Action<IGraphQLBuilder>? configure)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        configure?.Invoke(this);
        RegisterDefaultServices();
        // Registry.For<IServiceProvider>(Lifecycles.Container).Use<StructureMapResolver>();
    }

    private static ILifecycle TranslateLifetime(ServiceLifetime serviceLifetime)
    {
        return serviceLifetime switch
        {
            ServiceLifetime.Singleton => Lifecycles.Singleton,
            ServiceLifetime.Scoped => Lifecycles.Singleton, //TODO,
            ServiceLifetime.Transient => Lifecycles.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(serviceLifetime))
        };
    }

    public IServiceRegister Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
        where TOptions : class, new()
    {
        return this;
    }

    public IServiceRegister Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).ClearAll().Use(implementationType);
        }
        else
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).Use(implementationType);
        }

        return this;
    }

    public IServiceRegister Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).ClearAll().Use(context => implementationFactory(context.GetInstance<IServiceProvider>()));
        }
        else
        {
            Registry.For(serviceType, TranslateLifetime(serviceLifetime)).Use(context => implementationFactory(context.GetInstance<IServiceProvider>()));
        }

        return this;
    }

    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = false)
    {
        if (replace)
        {
            Registry.For(serviceType, Lifecycles.Singleton).ClearAll().Use(implementationInstance);
        }
        else
        {
            Registry.For(serviceType, Lifecycles.Singleton).Use(implementationInstance);
        }

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));

        //var descriptor = new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime));
        if (mode == RegistrationCompareMode.ServiceType)
        {
            //  ServiceCollection.TryAdd(descriptor);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // ServiceCollection.TryAddEnumerable(descriptor);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationFactory == null)
            throw new ArgumentNullException(nameof(implementationFactory));

        // var descriptor = new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime));
        if (mode == RegistrationCompareMode.ServiceType)
        {
            // ServiceCollection.TryAdd(descriptor);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // ServiceCollection.TryAddEnumerable(descriptor);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }

    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));
        if (implementationInstance == null)
            throw new ArgumentNullException(nameof(implementationInstance));

        //var descriptor = new ServiceDescriptor(serviceType, implementationInstance);
        if (mode == RegistrationCompareMode.ServiceType)
        {
            // ServiceCollection.TryAdd(descriptor);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // ServiceCollection.TryAddEnumerable(descriptor);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(mode));

        return this;
    }
}
