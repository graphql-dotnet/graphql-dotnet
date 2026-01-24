namespace GraphQL.Types;

/// <summary>
/// Represents a graph type that, for output graphs, is never <see langword="null"/>, or for input graphs, is not optional.
/// In other words the NonNull type wraps another type, and denotes that the resulting value will never be <see langword="null"/>.
/// </summary>
public class NonNullGraphType : GraphType, IProvideResolvedType
{
    /// <summary>
    /// Initializes a new instance for the specified inner graph type.
    /// </summary>
    public NonNullGraphType(IGraphType type)
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
            if (value is NonNullGraphType) //TODO: null check here or in ctor
            {
                // https://spec.graphql.org/October2021/#sec-Non-Null.Type-Validation
                throw new ArgumentOutOfRangeException("ResolvedType", "Cannot nest NonNull inside NonNull.");
            }

            field = value;
            _cachedString = null;
        }
    }

    private string? _cachedString; // note, than Name always null for type wrappers

    /// <inheritdoc/>
    public override string ToString() => _cachedString ??= $"{ResolvedType}!";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (NonNullGraphType)obj;
        return Equals(ResolvedType, other.ResolvedType);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => ToString().GetHashCode();
}

/// <inheritdoc cref="NonNullGraphType"/>
public sealed class NonNullGraphType<T> : NonNullGraphType
    where T : IGraphType
{
    private NonNullGraphType() : base(null!) { }
}
