using GraphQL.Types;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public static class FieldResolverBuilderExtensions
    {
        public static IFieldMiddlewareBuilder Use<T>(this IFieldMiddlewareBuilder builder) where T : IFieldMiddleware, new()
        {
            return builder.Use(next => context => (new T()).Resolve(context, next));
        }

        public static IFieldMiddlewareBuilder Use(this IFieldMiddlewareBuilder builder, IFieldMiddleware middleware)
        {
            return builder.Use(next => context => middleware.Resolve(context, next));
        }
    }
}
