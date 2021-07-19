using System;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Extension methods for <see cref="IFieldResolver"/> instances.
    /// </summary>
    public static class FieldResolverExtensions
    {
        /// <summary>
        /// Executes a field resolver with a specified <see cref="IResolveFieldContext"/>.
        /// </summary>
        public static async Task<object?> ResolveAsync(this IFieldResolver resolver, IResolveFieldContext context)
        {
            object? result = (resolver ?? throw new ArgumentNullException(nameof(resolver))).Resolve(context);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                result = task.GetResult();
            }

            return result;
        }
    }
}
