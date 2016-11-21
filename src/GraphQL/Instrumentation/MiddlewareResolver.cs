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
            _next = next ?? new NameFieldResolver();
        }

        public async Task<object> Resolve(ResolveFieldContext context)
        {
            var result = _next.Resolve(context);

            if (result is Task)
            {
                var task = result as Task;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                await task.ConfigureAwait(false);
                result = task.GetProperyValue("Result");
            }

            return result;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
