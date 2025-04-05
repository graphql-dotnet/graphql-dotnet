namespace GraphQL.Types;

/// <summary>
/// Represents a GraphQL interface graph type.
/// </summary>
public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType, IImplementInterfaces
{
}

// todo: merge these members into IAbstractGraphType for v9 (which already match the members in UnionGraphType)
internal interface IInterfaceGraphType2 : IInterfaceGraphType
{
    public IEnumerable<Type> Types { get; set; }
    public void Type(Type type);
    public void Type<TType>() where TType : IObjectGraphType;
}

/// <inheritdoc cref="InterfaceGraphType"/>
public class InterfaceGraphType<[NotAGraphType] TSource> : ComplexGraphType<TSource>, IInterfaceGraphType2
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public InterfaceGraphType()
        : this(null)
    {
    }

    internal InterfaceGraphType(InterfaceGraphType<TSource>? copyFrom)
        : base(copyFrom)
    {
        if (copyFrom != null)
        {
            if (copyFrom.PossibleTypes.Count != 0)
                throw new InvalidOperationException("Cannot clone interface containing possible types.");
            if (copyFrom.ResolveType != null)
                throw new InvalidOperationException("Cannot clone interface with configured ResolveType property.");
            _types.AddRange(copyFrom._types);
        }
        // else { /* initialization logic */ }
    }

    /// <inheritdoc/>
    public PossibleTypes PossibleTypes { get; } = new PossibleTypes();

    private List<Type> _types = new();
    /// <inheritdoc cref="PossibleTypes"/>
    public IEnumerable<Type> Types
    {
        get => _types;
        set => _types = new(value);
    }

    /// <inheritdoc/>
    public Func<object, IObjectGraphType?>? ResolveType { get; set; }

    /// <inheritdoc/>
    public void AddResolvedInterface(IInterfaceGraphType graphType)
    {
        if (graphType == null)
            throw new ArgumentNullException(nameof(graphType));

        _ = graphType.IsValidInterfaceFor(this, throwError: true);
        ResolvedInterfaces.Add(graphType);
    }

    /// <inheritdoc/>
    public Interfaces Interfaces { get; } = new Interfaces();

    /// <inheritdoc/>
    public ResolvedInterfaces ResolvedInterfaces { get; } = new ResolvedInterfaces();

    /// <inheritdoc/>
    public void AddPossibleType(IObjectGraphType type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        this.IsValidInterfaceFor(type, throwError: true);
        PossibleTypes.Add(type);
    }

    /// <inheritdoc cref="IAbstractGraphType.AddPossibleType"/>
    public void Type<TType>()
        where TType : IObjectGraphType
    {
        if (!_types.Contains(typeof(TType)))
            _types.Add(typeof(TType));
    }

    /// <inheritdoc cref="IAbstractGraphType.AddPossibleType"/>
    public void Type(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!typeof(IObjectGraphType).IsAssignableFrom(type))
            throw new ArgumentException($"Added type '{type.Name}' must implement {nameof(IObjectGraphType)}", nameof(type));

        if (!_types.Contains(type))
            _types.Add(type);
    }

    /// <summary>
    /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
    /// </summary>
    public void Interface<TInterface>()
        where TInterface : IInterfaceGraphType
        => Interfaces.Add<TInterface>();

    /// <summary>
    /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
    /// </summary>
    public void Interface(Type type) => Interfaces.Add(type);
}

/// <inheritdoc cref="IInterfaceGraphType"/>
public class InterfaceGraphType : InterfaceGraphType<object>
{
}
