using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class MiddlewareResolver : IFieldResolverInternal
    {
        private readonly FieldMiddlewareDelegate _func;

        public MiddlewareResolver(IFieldResolverInternal next)
        {
            next = next ?? NameFieldResolver.Instance;
            _func = (context) => next.SetResultAsync(context);
        }

        public MiddlewareResolver(FieldMiddlewareDelegate func)
        {
            _func = func;
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            return _func(context);
        }
    }
}
