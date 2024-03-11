namespace GraphQL.Conversion;

/// <inheritdoc cref="IListConverter"/>
public sealed class ListConverter : IListConverter
{
    private readonly Func<object?[], object> _converter;

    /// <inheritdoc cref="ListConverter"/>
    public ListConverter(Type elementType, Func<object?[], object> converter)
    {
        ElementType = elementType;
        _converter = converter;
    }

    /// <inheritdoc/>
    public Type ElementType { get; }

    /// <inheritdoc/>
    public object Convert(object?[] list) => _converter(list);
}
