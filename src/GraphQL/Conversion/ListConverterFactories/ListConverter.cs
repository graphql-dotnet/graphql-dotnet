
namespace GraphQL.Conversion;

/// <inheritdoc cref="IListConverter"/>
public sealed class ListConverter : IListConverter
{
    private readonly Func<object?[], object> _conversion;

    /// <inheritdoc cref="ListConverter"/>
    public ListConverter(Type elementType, Func<object?[], object> conversion)
    {
        ElementType = elementType;
        _conversion = conversion;
    }

    /// <inheritdoc/>
    public Type ElementType { get; }

    /// <inheritdoc/>
    public object Convert(object?[] list) => _conversion(list);
}
