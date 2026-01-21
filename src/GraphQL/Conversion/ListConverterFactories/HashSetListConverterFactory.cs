namespace GraphQL.Conversion;

/// <summary>
/// A converter that can convert a list of objects to a <see cref="HashSet{T}"/>.
/// </summary>
internal sealed class HashSetListConverterFactory : ListConverterFactoryBase
{
    private HashSetListConverterFactory()
    {
    }

    /// <inheritdoc cref="HashSetListConverterFactory"/>
    public static HashSetListConverterFactory Instance
    {
        [RequiresUnreferencedCode("Uses reflection to access generic method information which may be trimmed.")]
        [RequiresDynamicCode("Uses reflection to create generic method signatures.")]
        get;
    } = new();

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
