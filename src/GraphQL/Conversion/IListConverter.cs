namespace GraphQL.Conversion;

/// <summary>
/// A converter that can convert a list of objects to a specific list type, such as <see cref="HashSet{T}"/>.
/// </summary>
public interface IListConverter
{
    /// <summary>
    /// The type of the list element.
    /// </summary>
    Type ElementType { get; }

    /// <summary>
    /// Converts a list of objects to a specific list type, such as <see cref="HashSet{T}"/>.
    /// </summary>
    object Convert(object?[] list);
}
