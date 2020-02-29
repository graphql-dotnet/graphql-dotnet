using System;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    public static class FieldResolverExtensions
    {
        public static async Task<object> ResolveAsync(this IFieldResolver resolver, IResolveFieldContext context)
        {
            object result = (resolver ?? throw new ArgumentNullException(nameof(resolver))).Resolve(context);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                result = task.GetResult();
            }

            return result;
        }
    }
}
