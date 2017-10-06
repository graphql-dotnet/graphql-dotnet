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

            //more performant if available
            if (result is Task<object>)
            {
                var task = result as Task<object>;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                result = task.Result;
            }

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

        public bool RunThreaded()
        {
            return _next.RunThreaded();
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
