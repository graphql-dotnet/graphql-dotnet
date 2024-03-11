namespace GraphQL.Conversion;

internal sealed class HashSetListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> Create<T>() => static list =>
    {
#if NETSTANDARD2_0
        var hashSet = new HashSet<T>();
#else
        var hashSet = new HashSet<T>(list.Length);
#endif
        for (var i = 0; i < list.Length; i++)
        {
            hashSet.Add(list[i] == null ? default! : (T)list[i]!);
        }
        return hashSet;
    };
}
