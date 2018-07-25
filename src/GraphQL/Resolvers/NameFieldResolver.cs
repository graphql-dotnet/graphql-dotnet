using System;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    internal class NameFieldResolver : IFieldResolver
    {
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

            var propertyValue = source.GetPropertyValue(name);

            if (propertyValue == null)
            {
                throw new InvalidOperationException($"Expected to find property {name} on {source.GetType().Name} but it does not exist.");
            }

            return propertyValue;
        }
    }
}
