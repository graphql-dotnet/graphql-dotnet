namespace GraphQL.Conversion;

/// <summary>
/// A converter that can convert a list of objects to a typed array.
/// </summary>
/// <remarks>
/// This converter is fully compatible with AOT compilation when the requested type is an array type.
/// For interface types such as IEnumerable&lt;int&gt;, this class is compatible with AOT compilation
/// so long as the necessary array type is not trimmed.
/// </remarks>
internal sealed class ArrayListConverterFactory : IListConverterFactory
{
    private ArrayListConverterFactory()
    {
    }

    /// <inheritdoc cref="ArrayListConverterFactory"/>
    public static ArrayListConverterFactory Instance
    {
        [RequiresDynamicCode("Creates array types dynamically when the requested list type is not an array.")]
        get;
    } = new();

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "ArrayListConverterFactory.Instance is marked with [RequiresDynamicCode].")]
    public IListConverter Create(Type listType)
    {
        if (listType == typeof(object[]))
        {
            return new ListConverter(typeof(object), static list => list);
        }

        // determine the underlying element type
        var elementType = listType.GetListElementType();

        // for reference types or nullable value types, just copy the list to a new array
        if (!elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null)
        {
            return new ListConverter(elementType, list =>
            {
                var newArray = Array.CreateInstance(elementType, list.Length);
                list.CopyTo(newArray, 0);
                return newArray;
            });
        }

        // for non-nullable value types, coerce null to default(T)
        var elementDefault = GetDefaultValueTypeElement(elementType);

        // then return a converter that coerces null to default(T)
        return new ListConverter(elementType, list =>
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == null)
                {
                    list[i] = elementDefault;
                }
            }
            var newArray = Array.CreateInstance(elementType, list.Length);
            list.CopyTo(newArray, 0);
            return newArray;
        });
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "For value types, Activator.CreateInstance returns default(T) regardless of whether a constructor exists.")]
    private object? GetDefaultValueTypeElement(Type type)
    {
        return Activator.CreateInstance(type);
    }
}
