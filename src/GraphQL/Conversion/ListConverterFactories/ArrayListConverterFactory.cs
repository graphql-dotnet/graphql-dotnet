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
    public static ArrayListConverterFactory Instance { get; } = new();

    public IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        if (listType == typeof(object[]))
        {
            return new ListConverter(typeof(object), static list => list);
        }

        // determine the underlying element type
        var elementType = listType.GetListElementType()
            ?? throw new InvalidOperationException("The list type must be an array type.");

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
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        var elementDefault = Activator.CreateInstance(elementType);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.

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
}
