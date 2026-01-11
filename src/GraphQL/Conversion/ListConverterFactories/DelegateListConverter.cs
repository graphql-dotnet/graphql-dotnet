namespace GraphQL.Conversion;

/// <summary>
/// A converter that can convert a list of objects to a specific list type, such as <c>HashSet&lt;int&gt;</c>,
/// via a delegate provided to the constructor.
/// </summary>
/// <remarks>
/// This converter does not use reflection and is fully compatible with AOT scenarios.
/// </remarks>
internal class DelegateListConverter<TListType, TElementType> : IListConverter, IListConverterFactory
    where TListType : IEnumerable<TElementType>
{
    private readonly Func<object?[], object> _converter;

    public DelegateListConverter(Func<IEnumerable<TElementType>, TListType> converter)
    {
        _converter = arr => converter(CastOrDefault<TElementType>(arr));
    }

    public Type ElementType { get; } = typeof(TElementType);

    public object Convert(object?[] list) => _converter(list);

    public IListConverter Create(Type listType) => this;

    /// <summary>
    /// Casts each item in the array to the specified type, returning the default value for null items.
    /// </summary>
    private static IEnumerable<T> CastOrDefault<T>(object?[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            var value = source[i];
            yield return value == null ? default! : (T)value;
        }
    }
}
