using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<Task<TReturnType>>, IFieldResolverInternal
    {
        private readonly Func<IResolveFieldContext, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            _resolver = resolver;
        }

        public Task<TReturnType> Resolve(IResolveFieldContext context)
        {
            return _resolver(context);
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }

        public async Task SetResultAsync(IResolveFieldContext context)
        {
            context.Result = await _resolver(context);
        }
    }

    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolverInternal
    {
        private readonly Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public async Task SetResultAsync(IResolveFieldContext context)
        {
            context.Result = await _resolver(context.As<TSourceType>());
        }
    }
}
