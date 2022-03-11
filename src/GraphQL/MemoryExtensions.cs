using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for working with arrays and pools.
    /// </summary>
    public static class MemoryExtensions
    {
        private static readonly ConcurrentDictionary<Type, Action<Array>> _delegates = new ConcurrentDictionary<Type, Action<Array>>();
        private static readonly Func<Type, Action<Array>> _factory = CreateDelegate;

        internal static void Return(this Array array) => _delegates.GetOrAdd(array.GetType(), _factory)(array);

        // 'ArrayPool.Return' method takes generic T[] parameter for returned array, therefore it is required
        // to generate a method-adapter which takes 'Array' parameter and then casts it to the required type.
        //
        // Example:
        //
        // arr => ArrayPool<ElementType>.Shared.Return((ElementType[])arr, true)
        private static Action<Array> CreateDelegate(Type arrayType)
        {
            var poolType = typeof(System.Buffers.ArrayPool<>).MakeGenericType(arrayType.GetElementType()!);
            var parameter = Expression.Parameter(typeof(Array), "arr");

            var lambda = Expression.Lambda<Action<Array>>(
                Expression.Call(
                    Expression.Property(null, poolType.GetProperty(nameof(System.Buffers.ArrayPool<object>.Shared))!),
                    poolType.GetMethod(nameof(System.Buffers.ArrayPool<object>.Return))!,
                Expression.Convert(parameter, arrayType),
                Expression.Constant(true, typeof(bool))), parameter);

            return lambda.Compile();
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
}
