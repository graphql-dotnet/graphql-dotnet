namespace GraphQL.Types;

/// <summary>
/// An interface for such graph types that do not represent concrete graph types, that is, for interfaces and unions.
/// </summary>
public interface IAbstractGraphType : IGraphType
{
    /// <summary>
    /// Gets or sets a delegate that can be used to determine the proper graph type for the specified object value. See
    /// <see cref="AbstractGraphTypeExtensions.GetObjectType(IAbstractGraphType, object, ISchema)"/> for more details.
    /// </summary>
    public Func<object, IObjectGraphType?>? ResolveType { get; set; }

    /// <summary>
    /// Returns a set of possible types for this abstract graph type.
    /// </summary>
    public PossibleTypes PossibleTypes { get; }

    /// <summary>
    /// Adds the specified graph type to a list of possible graph types for this abstract graph type.
    /// </summary>
    public void AddPossibleType(IObjectGraphType type);
}
