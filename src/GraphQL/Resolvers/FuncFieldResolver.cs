using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class FuncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        public Task<TReturnType> ResolveAsync(ResolveFieldContext context)
        {
            return Task.FromResult(_resolver(context));
        }

        Task<object> IFieldResolver.ResolveAsync(ResolveFieldContext context)
        {
            return Task.FromResult((object)_resolver(context));
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task<TReturnType> ResolveAsync(ResolveFieldContext context)
        {
            return Task.FromResult(_resolver(context.As<TSourceType>()));
        }

        Task<object> IFieldResolver.ResolveAsync(ResolveFieldContext context)
        {
            return Task.FromResult((object)_resolver(context.As<TSourceType>()));
        }
    }
}
