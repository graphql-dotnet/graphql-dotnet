using System;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Utilities;

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

        public TReturnType Resolve(ExecutionContext context, ExecutionNode node)
        {
            var resolveContext = context.CreateResolveFieldContext(node);
            return Resolve(resolveContext);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }

        object IFieldResolver.Resolve(ExecutionContext context, ExecutionNode node)
        {
            return Resolve(context, node);
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            }
            _resolver = resolver;
        }

        public TReturnType Resolve(ResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        public TReturnType Resolve(ExecutionContext context, ExecutionNode node)
        {
            var resolveContext = context.CreateResolveFieldContext(node);
            return Resolve(resolveContext);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }

        object IFieldResolver.Resolve(ExecutionContext context, ExecutionNode node)
        {
            return Resolve(context, node);
        }
    }
}
