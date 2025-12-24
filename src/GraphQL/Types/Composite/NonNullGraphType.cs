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
            if (value is NonNullGraphType) //TODO: null check here or in ctor
            {
                // https://spec.graphql.org/October2021/#sec-Non-Null.Type-Validation
                throw new ArgumentOutOfRangeException("ResolvedType", "Cannot nest NonNull inside NonNull.");
            }

            if (value != null && Type != null && !Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentOutOfRangeException("ResolvedType", $"Type '{Type.Name}' should be assignable from ResolvedType '{value.GetType().Name}'.");

            field = value;
            _cachedString = null;
        }
    }

    private string? _cachedString; // note, than Name always null for type wrappers

    /// <inheritdoc/>
    public override string ToString() => _cachedString ??= $"{ResolvedType}!";
}

/// <inheritdoc cref="NonNullGraphType"/>
public sealed class NonNullGraphType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : NonNullGraphType
    where T : IGraphType
{
    /// <summary>
    /// Initializes a new instance for the specified inner graph type.
    /// </summary>
    [Obsolete("This constructor is for internal use only; use NonNullGraphType(IGraphType type) instead.")]
    public NonNullGraphType()
        : base(null!)
    {
        if (typeof(NonNullGraphType).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentOutOfRangeException("type", "Cannot nest NonNull inside NonNull.");
        }
    }

    /// <inheritdoc/>
    public override Type Type => typeof(T);
}
