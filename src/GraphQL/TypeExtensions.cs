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
            if (useInTypeName && type.Name.StartsWith("Root"))
            {
                return string.Empty;
            }
            return type.Name.EndsWith("Type") ? type.Name.Remove(type.Name.Length - 4) : type.Name;
        }
    }
}
