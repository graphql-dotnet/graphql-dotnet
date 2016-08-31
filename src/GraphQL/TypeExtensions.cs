using System;
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

        public static bool IsGraphType(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(IGraphType));
        }

        public static string GraphQLName(this Type type)
        {
            string typeName = type.Name;

            if (type.IsGenericType)
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

            if (type == typeof(string))
            {
                graphType = typeof(StringGraphType);
            }

            if (type == typeof(bool))
            {
                graphType = typeof(BooleanGraphType);
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
