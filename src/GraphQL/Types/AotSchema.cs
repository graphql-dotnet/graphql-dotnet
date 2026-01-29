using System.Collections.Concurrent;
using GraphQL.DI;
using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// Base class for AOT (Ahead-Of-Time) compiled schemas.
/// </summary>
public abstract class AotSchema : Schema, IServiceProvider
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="AotSchema"/> class.
    /// </summary>
    protected AotSchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations) : base(services, configurations, null, new ValueConverterAot(), [])
    {
        _services = services;
    }

    /// <summary>
    /// AOT type factories.
    /// </summary>
    protected Dictionary<Type, Func<object>> AotTypes { get; } = new();

    /// <summary>
    /// Registers an AOT graph type.
    /// </summary>
    protected void AddAotType<TGraphType>()
        where TGraphType : IGraphType, new()
    {
        AotTypes.Add(typeof(TGraphType), () => new TGraphType());
    }

    /// <summary>
    /// Registers an AOT graph type.
    /// </summary>
    protected void AddAotType<TGraphType, TGraphTypeImplementation>()
        where TGraphType : IGraphType
        where TGraphTypeImplementation : IGraphType, new()
    {
        AotTypes.Add(typeof(TGraphType), () => new TGraphTypeImplementation());
    }

    /// <summary>
    /// Gets a required service of the specified type.
    /// </summary>
    protected T GetRequiredService<T>()
        => ((IServiceProvider)this).GetRequiredService<T>();

    private readonly ConcurrentDictionary<Type, object> _serviceCache = new();
    object? IServiceProvider.GetService(Type serviceType)
    {
        if (_serviceCache.TryGetValue(serviceType, out var service))
        {
            return service;
        }
        if (AotTypes.TryGetValue(serviceType, out var factory))
        {
            service = factory();
            return _serviceCache.GetOrAdd(serviceType, service);
        }
        return _services.GetService(serviceType);
    }

    private static readonly (Type, Type)[] _builtInTypeMappings = [
        (typeof(int), typeof(IntGraphType)),
        (typeof(string), typeof(StringGraphType)),
        (typeof(bool), typeof(BooleanGraphType)),
        (typeof(double), typeof(FloatGraphType))
    ];

    /// <summary>
    /// Built-in type mappings for AOT schemas.
    /// </summary>
    public override IEnumerable<(Type clrType, Type graphType)> BuiltInTypeMappings => _builtInTypeMappings;
}
