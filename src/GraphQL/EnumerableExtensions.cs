using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<object> Map(this IEnumerable items, Func<object, object> map)
        {
            var mappedItems = from object item in items select map(item);
            return mappedItems.ToList();
        }

        public static async Task<IEnumerable> MapAsync(this IEnumerable items, Func<object, Task<object>> map)
        {
            var tasks = items
                .Cast<object>()
                .Select(map)
                .ToArray();

            return await Task.WhenAll(tasks);
        }

        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }
        
        public static async Task<Dictionary<TKey, TValueVal>> ToDictionaryAsync<TSource, TKey, TValue, TValueVal>(
            this IEnumerable<TSource> items,
            Func<TSource, TKey> keyFunc,
            Func<TSource, Task<TValue>> valueFunc) where TValue : ResolveFieldResult<TValueVal>
        {
            var tasks = items
                .Select(async item => new {
                    Key = keyFunc(item),
                    Result = await valueFunc(item)
                })
                .ToArray();

            var keyValuePairs = await Task.WhenAll(tasks);
            keyValuePairs = keyValuePairs.Where(x => !x.Result.Skip).ToArray();

            return keyValuePairs.ToDictionary(kvp => kvp.Key, kvp => kvp.Result.Value);
        }

        public static bool Any<T>(this IEnumerable<T> items, Func<T, bool> check)
        {
            var result = false;

            foreach (var item in items)
            {
                result |= check(item);

                if (result)
                {
                    break;
                }
            }

            return result;
        }

        public static bool Any(this IEnumerable items, Func<object, bool> check)
        {
            var result = false;

            foreach (var item in items)
            {
                result |= check(item);

                if (result)
                {
                    break;
                }
            }

            return result;
        }

        public static bool All(this IEnumerable items, Func<object, bool> check)
        {
            var result = true;

            foreach (var item in items)
            {
                result &= check(item);
                if (!result)
                {
                    break;
                }
            }

            return result;
        }

        public static void Fill<T>(this IList<T> items, T itemToAdd)
        {
            Fill(items, new[] { itemToAdd });
        }

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
