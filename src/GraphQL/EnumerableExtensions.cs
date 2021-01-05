using System;
using System.Collections.Generic;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for working with <see cref="IEnumerable{T}"/> lists.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs the indicated action on each item.
        /// </summary>
        /// <param name="items">The list of items to act on.</param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        /// <summary>
        /// Performs the indicated action on each item. Boxing free for <c>List+Enumerator{T}</c>.
        /// </summary>
        /// <param name="items">The list of items to act on.</param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this List<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        /// <summary>
        /// Performs the indicated action on each key-value pair.
        /// </summary>
        /// <param name="items">The dictionary of items to act on.</param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        [Obsolete]
        public static void Apply(this System.Collections.IDictionary items, Action<object, object> action)
        {
            foreach (object key in items.Keys)
            {
                action(key, items[key]);
            }
        }
    }
}
