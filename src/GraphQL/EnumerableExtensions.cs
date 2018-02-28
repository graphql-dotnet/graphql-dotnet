using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Equivalent to .Select(...).ToList()
        /// </summary>
        public static IEnumerable<TK> Map<T, TK>(this IEnumerable<T> items, Func<T, TK> map)
        {
            var mappedItems = from T item in items select map(item);
            return mappedItems.ToList();
        }

        /// <summary>
        /// Equivalent to .Select(...).ToList()
        /// </summary>
        public static IEnumerable<T> Map<T>(this IEnumerable items, Func<object, T> map)
        {
            var mappedItems = from object item in items select map(item);
            return mappedItems.ToList();
        }

        /// <summary>
        /// Equivalent to .Select(...).ToList(), except the Select function is executed asynchronously. 
        /// </summary>
        public static Task<object[]> MapAsync(this IEnumerable items, Func<object, Task<object>> map)
        {
            var tasks = items
                .Cast<object>()
                .Select(map);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Equivalent to .Select(...).ToList()
        /// </summary>
        public static async Task<IEnumerable<object>> MapAsync(this IEnumerable enumerable, Func<int, object, Task<object>> mapFunction)
        {
            var index = 0;
            var results = new List<object>();

            foreach (var item in enumerable)
            {
                var result = await mapFunction(index++, item);

                results.Add(result);
            }

            return results;

            //return await enumerable
            //    .Cast<object>()
            //    .Select((item, index) => Tuple.Create(index, item))
            //    .MapAsync(async tuple =>
            //    {
            //        var data = (Tuple<int, object>)tuple;
            //        return await mapFunction(data.Item1, data.Item2).ConfigureAwait(false);
            //    })
            //    .ConfigureAwait(false);
        }

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

        /// <summary>
        /// Performs the indicated action on each item.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this IEnumerable<T> items, Action<T, int> action)
        {
            var count = 0;
            foreach (var item in items)
            {
                action(item, count);
                count++;
            }
        }

        /// <summary>
        /// Performs the indicated action on each item in reverse order.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void ApplyReverse<T>(this IEnumerable<T> items, Action<T> action)
        {
            var list = items.ToList();

            for (var i = list.Count - 1; i >= 0; i--)
            {
                action(list[i]);
            }
        }

        public static bool All(this IEnumerable items, Func<object, bool> check)
        {
            foreach (var item in items)
            {
                if (!check(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds the item to the list, unless the list already contains the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list to be updated.</param>
        /// <param name="itemToAdd">The item to be conditionally added.</param>
        public static void Fill<T>(this IList<T> items, T itemToAdd)
        {
            Fill(items, new[] { itemToAdd });
        }


        /// <summary>
        /// Adds each item to the list, unless the list already contains the item.
        /// </summary>
        /// <param name="items">The list to be updated.</param>
        /// <param name="itemsToAdd">The items to be conditionally added.</param>
        public static void Fill<T>(this IList<T> items, IEnumerable<T> itemsToAdd)
        {
            itemsToAdd.Apply(x =>
            {
                if (!items.Contains(x))
                {
                    items.Add(x);
                }
            });
        }
    }
}
