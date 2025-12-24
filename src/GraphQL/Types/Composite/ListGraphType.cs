namespace GraphQL.Types;

/// <summary>
/// Represents a list of objects. A GraphQL schema may describe that a field represents a list of another type.
/// The List type is provided for this reason, and wraps another type.
/// </summary>
public class ListGraphType : GraphType, IProvideResolvedType
{
    /// <summary>
    /// Initializes a new instance for the specified inner graph type.
    /// </summary>
    public ListGraphType(IGraphType type)
    {
        ResolvedType = type;
    }

    /// <summary>
    /// Returns the .NET type of the inner (wrapped) graph type.
    /// </summary>
    public virtual Type? Type => null;

    /// <summary>
    /// Gets or sets the instance of the inner (wrapped) graph type.
    /// </summary>
    public IGraphType? ResolvedType
    {
        get;
        set
        {
            if (value != null && Type != null && !Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentOutOfRangeException("ResolvedType", $"Type '{Type.Name}' should be assignable from ResolvedType '{value.GetType().Name}'.");

            field = value;
            _cachedString = null;
        }
    }

    private string? _cachedString; // note, than Name always null for type wrappers

    /// <inheritdoc/>
    public override string ToString() => _cachedString ??= $"[{ResolvedType}]";
}

/// <inheritdoc cref="ListGraphType"/>
public sealed class ListGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : ListGraphType
    where T : IGraphType
{
    /// <summary>
    /// Initializes a new instance for the specified inner graph type.
    /// </summary>
    [Obsolete("This constructor is for internal use only; use ListGraphType(IGraphType type) instead.")]
    public ListGraphType()
        : base(null!)
    {
    }

    /// <inheritdoc/>
    public override Type Type => typeof(T);
}
