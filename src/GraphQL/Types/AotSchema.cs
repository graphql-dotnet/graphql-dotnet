using GraphQL.DI;

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

    object? IServiceProvider.GetService(Type serviceType)
    {
        if (AotTypes.TryGetValue(serviceType, out var factory))
        {
            return factory();
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
