using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

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

        public Task<object> Resolve(ExecutionContext context, ExecutionNode node)
        {
            var resolveContext = context.CreateResolveFieldContext(node);
            return Resolve(resolveContext);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }

        object IFieldResolver.Resolve(ExecutionContext context, ExecutionNode node)
        {
            return Resolve(context, node);
        }
    }
}
