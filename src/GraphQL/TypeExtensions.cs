using System;
using GraphQL.Types;

namespace GraphQL
{
    public static class TypeExtensions
    {
        public static T As<T>(this object item)
            where T : class
        {
            return item as T;
        }

        public static string GraphQLName(this Type type, bool useInTypeName = false)
        {
            if (useInTypeName && type.Name == nameof(ObjectGraphType))
            {
                return string.Empty;
            }
            var typeName = type.Name.Replace(nameof(GraphType), nameof(Type));
            return typeName.EndsWith(nameof(Type))
                ? typeName.Remove(typeName.Length - nameof(Type).Length)
                : typeName;
        }
    }
}
