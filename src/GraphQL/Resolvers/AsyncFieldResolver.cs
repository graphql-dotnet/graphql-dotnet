using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class AsyncFieldResolver<TSourceType, TReturnType> : IAsyncFieldResolver
    {
        private readonly Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public async Task<object> Resolve(ResolveFieldContext context)
        {
            var result = _resolver(new ResolveFieldContext<TSourceType>(context));

            if (result == null)
                throw new InvalidOperationException(
                    "Resolver result is null when Task<object> is expected. When using resolvers with " +
                    "Task<object> and null results you must provide a valid return value Task<object>");

            return await result;
        }
    }
}
