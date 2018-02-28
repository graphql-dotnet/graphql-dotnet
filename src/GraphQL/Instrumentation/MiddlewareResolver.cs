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

        public Task<object> Resolve(ResolveFieldContext context)
        {
            object result = _next.Resolve(context);

            if (result is Task<object> task)
            {
                return task;
            }

            return Task.FromResult(result);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
