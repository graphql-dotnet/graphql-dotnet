using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class MiddlewareResolver : IFieldResolver<Task<object>>
    {
        private readonly IFieldResolver _next;

        public MiddlewareResolver(IFieldResolver next)
        {
            _next = next ?? NameFieldResolver.Instance;
        }

        public async Task<object> Resolve(IResolveFieldContext context)
        {
            object result = _next.Resolve(context);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                return task.GetResult();
            }
            else
            {
                return result;
            }
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
