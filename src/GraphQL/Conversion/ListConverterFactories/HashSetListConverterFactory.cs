namespace GraphQL.Conversion;

internal sealed class HashSetListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> Create<T>()
        => list => new HashSet<T>(list.Cast<T>());
}
