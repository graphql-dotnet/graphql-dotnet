namespace GraphQL.Analyzers.Helpers;

public static class EnumerableExtensions
{
    /// <summary>
    /// Creates a <see cref="HashSet{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <param name="source">The source <see cref="IEnumerable{T}"/>.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the elements from the source.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new(source);

    /// <summary>
    /// Creates a <see cref="HashSet{T}"/> from an <see cref="IEnumerable{T}"/> using the specified <see cref="IEqualityComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source.</typeparam>
    /// <param name="source">The source <see cref="IEnumerable{T}"/>.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> to use for comparing elements.</param>
    /// <returns>A <see cref="HashSet{T}"/> containing the elements from the source.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        => new(source, comparer);
}
