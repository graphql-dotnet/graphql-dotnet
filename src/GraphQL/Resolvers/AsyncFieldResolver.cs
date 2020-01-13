using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            _resolver = resolver;
        }

        public Task<TReturnType> ResolveAsync(IResolveFieldContext context)
        {
            return _resolver(context);
        }

        async Task<object> IFieldResolver.ResolveAsync(IResolveFieldContext context)
        {
            return await ResolveAsync(context).ConfigureAwait(false);
        }
    }

    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task<TReturnType> ResolveAsync(IResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        async Task<object> IFieldResolver.ResolveAsync(IResolveFieldContext context)
        {
            return await ResolveAsync(context).ConfigureAwait(false);
        }
    }
}
