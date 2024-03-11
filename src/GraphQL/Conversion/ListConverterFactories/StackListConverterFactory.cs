namespace GraphQL.Conversion;

internal sealed class StackListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> GetConversion<T>()
        => list => new Stack<T>(list.Cast<T>());
}
