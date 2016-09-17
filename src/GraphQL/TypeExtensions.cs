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

        public static IGraphType GetGraphTypeFromType(this Type type, bool isNullable = false)
        {
            IGraphType graphType = null;

            if (type == typeof(int))
            {
                graphType = ScalarGraphTypes.Int;
            }

            if (type == typeof(long))
            {
                graphType = ScalarGraphTypes.Int;
            }

            if (type == typeof(double))
            {
                graphType = ScalarGraphTypes.Float;
            }

            if (type == typeof(string))
            {
                graphType = ScalarGraphTypes.String;
            }

            if (type == typeof(bool))
            {
                graphType = ScalarGraphTypes.Boolean;
            }

            if (type == typeof(DateTime))
            {
                graphType = ScalarGraphTypes.Date;
            }

            if (graphType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Unknown input type.");
            }

            if (!isNullable)
            {
                graphType = new NonNullGraphType(graphType);
            }

            return graphType;
        }
    }
}
