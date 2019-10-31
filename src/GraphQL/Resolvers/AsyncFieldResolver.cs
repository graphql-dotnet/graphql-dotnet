using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext, Task<TReturnType>> resolver)
        {
            _resolver = resolver;
        }

        public Task<TReturnType> ResolveAsync(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        async Task<object> IFieldResolver.ResolveAsync(ResolveFieldContext context)
        {
            return await ResolveAsync(context).ConfigureAwait(false);
        }
    }

    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task<TReturnType> ResolveAsync(ResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        async Task<object> IFieldResolver.ResolveAsync(ResolveFieldContext context)
        {
            return await ResolveAsync(context).ConfigureAwait(false);
        }
    }
}
