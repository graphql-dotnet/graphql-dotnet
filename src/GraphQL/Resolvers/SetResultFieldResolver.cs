using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class SetResultFieldResolver : IFieldResolverInternal
    {
        private readonly Func<IResolveFieldContext, Task> _resolver;

        public SetResultFieldResolver(Func<IResolveFieldContext, Task> resolver)
        {
            _resolver = resolver;
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            return _resolver(context);
        }
    }
}
