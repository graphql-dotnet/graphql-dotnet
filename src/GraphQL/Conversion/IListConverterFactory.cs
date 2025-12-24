namespace GraphQL.Conversion;

/// <summary>
/// A factory that can return a converter for a specific list type, such as <see cref="HashSet{T}"/>.
/// </summary>
public interface IListConverterFactory
{
    /// <summary>
    /// Returns a converter that can be used to convert an array of objects to a specific list type,
    /// such as <see cref="HashSet{T}"/>.
    /// </summary>
    public IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType);
}
