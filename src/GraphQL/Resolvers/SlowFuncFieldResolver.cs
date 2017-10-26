using System;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    ///     Different from async in that we want to run this in a seperate thread to allow
    ///     the entire query to return faster. Note this does not apply to mutations since they
    ///     must run sequentially
    /// </summary>
    public class SlowFuncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext, TReturnType> _resolver;

        public SlowFuncFieldResolver(Func<ResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        public TReturnType Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        public bool RunThreaded()
        {
            return true;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class SlowFuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public SlowFuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("A resolver function must be specified");
            }
            _resolver = resolver;
        }

        public bool RunThreaded()
        {
            return true;
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
