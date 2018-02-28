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

            var prop = source.GetType()
                .GetProperty(name, _flags);

            if (prop == null)
            {
                throw new InvalidOperationException($"Expected to find property {name} on {source.GetType().Name} but it does not exist.");
            }

            return prop.GetValue(source, null);
        }
    }
}
