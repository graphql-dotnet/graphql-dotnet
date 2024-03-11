namespace GraphQL.Conversion;

internal sealed class ArrayListConverterFactory : ListConverterFactoryBase
{
    private ArrayListConverterFactory()
    {
    }

    public static ArrayListConverterFactory Instance { get; } = new ArrayListConverterFactory();

    public override IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        if (listType == typeof(object[]))
        {
            return new ListConverter(typeof(object), static list => list);
        }
        // for reference types, just copy the list to a new array
        var elementType = GetElementType(listType);
        if (!elementType.IsValueType)
        {
            return new ListConverter(elementType, list =>
            {
                var newArray = Array.CreateInstance(elementType, list.Length);
                list.CopyTo(newArray, 0);
                return newArray;
            });
        }
        // call Create<T> to create a converter for value types,
        // with logic coercing null to default(T)
        return base.Create(listType);
    }

    public override Func<object?[], object> Create<T>() => static list =>
    {
        var newArray = new T[list.Length];
        for (var i = 0; i < list.Length; i++)
        {
            var value = list[i];
            if (value != null)
            {
                newArray[i] = (T)value;
            }
        }
        return newArray;
    };
}
