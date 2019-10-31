using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public class MiddlewareResolver : IFieldResolver
    {
        private readonly IFieldResolver _next;

        public MiddlewareResolver(IFieldResolver next)
        {
            _next = next ?? NameFieldResolver.Instance;
        }

        public Task<object> ResolveAsync(ResolveFieldContext context)
        {
            return _next.ResolveAsync(context);
        }

    }
}
