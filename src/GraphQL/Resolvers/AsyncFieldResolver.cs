using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<ResolveFieldContext, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext, Task<TReturnType>> resolver)
        {
            _resolver = resolver;
        }

        public Task<TReturnType> Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException("A resolver function must be specified");
        }

        public Task<TReturnType> Resolve(ResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
