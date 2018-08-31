using System;
using System.Collections.Generic;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs the indicated action on each item.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }
    }
}
