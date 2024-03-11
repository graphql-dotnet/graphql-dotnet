namespace GraphQL.Conversion;

internal sealed class ArrayListConverterFactory : ListConverterFactoryBase
{
    private ArrayListConverterFactory()
    {
    }

    public static ArrayListConverterFactory Instance { get; } = new ArrayListConverterFactory();

    public override Func<object?[], object> GetConversion<T>()
    {
        if (typeof(T) == typeof(object))
        {
            return static list => list;
        }
        return static (list) =>
        {
            var newArray = Array.CreateInstance(typeof(T), list.Length);
            list.CopyTo(newArray, 0);
            return newArray;
        };
    }
}
