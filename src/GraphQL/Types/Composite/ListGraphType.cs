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
    /// Gets or sets the instance of the inner (wrapped) graph type.
    /// </summary>
    public IGraphType? ResolvedType
    {
        get;
        set
        {
            field = value;
            _cachedString = null;
        }
    }

    private string? _cachedString; // note, than Name always null for type wrappers

    /// <inheritdoc/>
    public override string ToString() => _cachedString ??= $"[{ResolvedType}]";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ListGraphType)obj;
        return Equals(ResolvedType, other.ResolvedType);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => ToString().GetHashCode();
}

/// <inheritdoc cref="ListGraphType"/>
public sealed class ListGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : ListGraphType
    where T : IGraphType
{
    private ListGraphType() : base(null!) { }
}
