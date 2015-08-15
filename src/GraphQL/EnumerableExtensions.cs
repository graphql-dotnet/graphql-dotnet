using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL
{
    public static class EnumerableExtensions
    {
        public static IEnumerable Map(this IEnumerable items, Func<object, object> map)
        {
            return from object item in items select map(item);
        }

        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
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
