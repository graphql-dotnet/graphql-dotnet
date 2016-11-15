using System;
using System.Collections.Generic;
using System.Reflection;
using GraphQL.Types;
using System.Linq;

namespace GraphQL
{
    public static class TypeExtensions
    {
        public static T As<T>(this object item)
            where T : class
        {
            return item as T;
        }

        public static bool IsConcrete(this Type type)
        {
            if (type == null) return false;

            var typeInfo = type.GetTypeInfo();

            return !typeInfo.IsAbstract && !typeInfo.IsInterface;
        }

        public static bool IsNullable(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Returns the first non-null value from executing the func against the enumerable
        /// </summary>
        public static TReturn FirstValue<TItem, TReturn>(this IEnumerable<TItem> enumerable, Func<TItem, TReturn> func)
            where TReturn : class
        {
            foreach (TItem item in enumerable)
            {
                TReturn @object = func(item);
                if (@object != null) return @object;
            }

            return null;
        }

        public static bool IsGraphType(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(IGraphType));
        }

		public static string GraphQLName(this Type type)
        {
            string typeName = type.Name;

            if (type.GetTypeInfo().IsGenericType)
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }

            typeName = typeName.Replace(nameof(GraphType), nameof(Type));

            return typeName.EndsWith(nameof(Type))
                ? typeName.Remove(typeName.Length - nameof(Type).Length)
                : typeName;
        }

        public static Type GetGraphTypeFromType(this Type type, bool isNullable = false)
        {
            Type graphType = null;

            if (type == typeof(int))
            {
                graphType = typeof(IntGraphType);
            }

            if (type == typeof(long))
            {
                graphType = typeof(IntGraphType);
            }

            if (type == typeof(double))
            {
                graphType = typeof(FloatGraphType);
            }

            if (type == typeof(decimal))
            {
                graphType = typeof(DecimalGraphType);
            }

            if (type == typeof(string))
            {
                graphType = typeof(StringGraphType);
            }

            if (type == typeof(bool))
            {
                graphType = typeof(BooleanGraphType);
            }

            if (type == typeof(DateTime))
            {
                graphType = typeof(DateGraphType);
            }

            if (graphType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Unknown input type.");
            }

            if (!isNullable)
            {
                var nullType = typeof(NonNullGraphType<>);
                graphType = nullType.MakeGenericType(graphType);
            }

            return graphType;
        }
    }
}
