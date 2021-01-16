using System;
using System.Threading.Tasks;
using GraphQL.Execution;

namespace GraphQL.Resolvers
{
    public delegate object OptimizedFunc<T>(ReadonlyResolveFieldContextStruct<T> arg);

    public delegate Task<object> OptimizedTaskFunc<T>(ReadonlyResolveFieldContextStruct<T> arg);

    internal sealed class OptimizedFuncFieldResolver<TSource> : IFieldResolver, IOptimizedFieldResolver
    {
        private readonly OptimizedFunc<TSource> _resolver;
        private readonly OptimizedTaskFunc<TSource> _asyncResolver;

        public OptimizedFuncFieldResolver(OptimizedFunc<TSource> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public OptimizedFuncFieldResolver(OptimizedTaskFunc<TSource> resolver)
        {
            _asyncResolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public object Resolve(ExecutionNode node, ExecutionContext context)
        {
            var c = new ReadonlyResolveFieldContextStruct<TSource>(node, context);
            return _resolver != null ? _resolver(c) : _asyncResolver(c);
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return context is ReadonlyResolveFieldContextStruct<TSource> typedContext
                ? _resolver(typedContext)
                : throw new ArgumentException($"Context must be of '{typeof(ReadonlyResolveFieldContextStruct<TSource>).Name}' type.", nameof(context));
        }
    }
}
