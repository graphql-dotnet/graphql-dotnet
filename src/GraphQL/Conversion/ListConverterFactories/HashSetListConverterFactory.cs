namespace GraphQL.Conversion;

internal sealed class HashSetListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => new HashSet<T>(list.Cast<T>());
}
