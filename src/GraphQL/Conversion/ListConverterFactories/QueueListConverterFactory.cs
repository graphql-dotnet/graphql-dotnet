namespace GraphQL.Conversion;

internal sealed class QueueListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => new Queue<T>(list.Cast<T>());
}
