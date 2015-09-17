using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        public static IEnumerable Map(this IEnumerable items, Func<object, object> map)
        {
            return from object item in items select map(item);
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
        
        public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TSource, TKey, TValue>(
            this IEnumerable<TSource> items,
            Func<TSource, TKey> keyFunc,
            Func<TSource, Task<TValue>> valueFunc)
        {
            var tasks = items
                .Select(async item => new {
                    Key = keyFunc(item),
                    Value = await valueFunc(item)
                })
                .ToArray();

            var keyValuePairs = await Task.WhenAll(tasks);

            return keyValuePairs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
    }
}
