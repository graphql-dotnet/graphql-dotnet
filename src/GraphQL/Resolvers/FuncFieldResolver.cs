using System;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class FuncFieldResolver<TReturnType> : IFieldResolver
    {
        private readonly Func<ResolveFieldContext, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        public object Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public object Resolve(ResolveFieldContext context)
        {
            return _resolver(new ResolveFieldContext<TSourceType>(context));
        }
    }
}
