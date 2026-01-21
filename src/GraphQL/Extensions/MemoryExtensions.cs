using System.Buffers;
using System.Collections.Concurrent;

namespace GraphQL;

/// <summary>
/// Provides extension methods for working with arrays and pools.
/// </summary>
public static class MemoryExtensions
{
    private static readonly ConcurrentDictionary<Type, Action<Array>> _delegates = new();

    internal static T[] Rent<T>(int count)
    {
        _delegates.TryAdd(typeof(T[]), static (array) => ArrayPool<T>.Shared.Return((T[])array));
        return ArrayPool<T>.Shared.Rent(count);
    }

    internal static void Return(this Array array)
    {
        if (_delegates.TryGetValue(array.GetType(), out var action))
            action(array);
    }

    /// <summary>
    /// Returns an array or array-like object of a given length.
    /// </summary>
    public static IList<T> Constrained<T>(this T[] array, int count)
    {
        if (count == 0)
            return Array.Empty<T>();

        if (array.Length == count)
            return array;

        return new ConstrainedArray<T>(array, count);
    }
}
