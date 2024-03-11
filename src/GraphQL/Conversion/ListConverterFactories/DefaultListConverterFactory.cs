namespace GraphQL.Conversion;

/// <summary>
/// A converter that can convert a list of objects to a <see cref="List{T}"/>.
/// </summary>
internal sealed class DefaultListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> Create<T>()
    {
        // simplified for reference types
        if (!typeof(T).IsValueType)
        {
            return list =>
            {
                var newList = new List<T>(list.Length);
                for (var i = 0; i < list.Length; i++)
                {
                    newList.Add((T)list[i]!);
                }
                return newList;
            };
        }
        // coerces null to default(T)
        return list =>
        {
            var newList = new List<T>(list.Length);
            for (var i = 0; i < list.Length; i++)
            {
                var value = list[i];
                newList.Add(value != null ? (T)value : default!);
            }
            return newList;
        };
    }
}
