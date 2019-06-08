using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public sealed class SyncToAsyncResolverAdapter : IFieldResolver
    {
        private readonly IFieldResolver _inner;

        public SyncToAsyncResolverAdapter(IFieldResolver inner)
        {
            _inner = inner;
        }

        public object Resolve(ResolveFieldContext context)
        {
            return ResolveAsync(context);
        }

        public async Task<object> ResolveAsync(ResolveFieldContext context)
        {
            var result = _inner.Resolve(context);

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                result = task.GetResult();
            }

            return result;
        }
    }
}
