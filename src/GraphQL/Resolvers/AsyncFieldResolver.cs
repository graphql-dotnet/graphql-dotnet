using System;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Utilities;

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

        public Task<TReturnType> Resolve(ExecutionContext context, ExecutionNode node)
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

    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task<TReturnType> Resolve(ResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        public Task<TReturnType> Resolve(ExecutionContext context, ExecutionNode node)
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
