using System;
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

        public TReturnType Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public TReturnType Resolve(ResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
