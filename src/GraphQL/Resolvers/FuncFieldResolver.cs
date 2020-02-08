using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class FuncFieldResolver<TReturnType> : IFieldResolverInternal
    {
        private readonly Func<IResolveFieldContext, TReturnType> _resolver;

        public FuncFieldResolver(Func<IResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            context.Result = _resolver(context);
            return Task.CompletedTask;
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolverInternal
    {
        private readonly Func<IResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            context.Result = _resolver(context.As<TSourceType>());
            return Task.CompletedTask;
        }
    }
}
