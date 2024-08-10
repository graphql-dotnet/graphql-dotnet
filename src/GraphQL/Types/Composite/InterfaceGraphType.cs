namespace GraphQL.Types;

/// <summary>
/// Represents a GraphQL interface graph type.
/// </summary>
public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType, IImplementInterfaces
{
}

/// <inheritdoc cref="InterfaceGraphType"/>
public class InterfaceGraphType<[NotAGraphType] TSource> : ComplexGraphType<TSource>, IInterfaceGraphType
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
        }
        // else { /* initialization logic */ }
    }

    /// <inheritdoc/>
    public PossibleTypes PossibleTypes { get; } = new PossibleTypes();

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
}

/// <inheritdoc cref="IInterfaceGraphType"/>
public class InterfaceGraphType : InterfaceGraphType<object>
{
}
