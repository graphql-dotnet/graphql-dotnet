namespace GraphQL;

internal static class EnumerableExtensions
{
    /// <inheritdoc cref="Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    public static bool FastAll<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        // .NET 9 already includes this optimization
#if !NET9_0_OR_GREATER
        // no-allocation check
        if (source is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (!predicate(list[i]))
                    return false;
            }
            return true;
        }
#endif
        // fallback to LINQ
        return source.All(predicate);
    }
}
