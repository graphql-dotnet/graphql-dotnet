using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;

namespace GraphQL.Types
{
    public static class ObjectGraphTypeExtensions
    {
        public static void Field(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            var field = new FieldType
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ResolvedType = type,
                Resolver = resolve != null ? new FuncFieldResolver<object>(resolve) : null
            };
            obj.AddField(field);
        }

        public static void FieldAsync(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, Task<object>> resolve = null)
        {
            var field = new FieldType
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ResolvedType = type,
                Resolver = resolve != null
                    ? new AsyncFieldResolver<object>(resolve)
                    : null
            };
            obj.AddField(field);
        }
    }
}
