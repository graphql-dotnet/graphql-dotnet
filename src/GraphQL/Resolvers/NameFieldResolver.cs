using System;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    internal class NameFieldResolver : IFieldResolver
    {
        private static readonly BindingFlags _flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

        public object Resolve(ResolveFieldContext context)
        {
            return Resolve(context?.Source, context?.FieldAst?.Name);
        }

        public static object Resolve(object source, string name)
        {
            if (source == null || name == null)
            {
                return null;
            }

            return source.GetPropertyValue(name);
        }
    }
}
