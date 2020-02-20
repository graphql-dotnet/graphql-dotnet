using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result.
    /// The returned value must be of an <see cref="Task{TResult}"/> type.
    /// </summary>
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<Task<TReturnType>>
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
    }

    /// <inheritdoc cref="AsyncFieldResolver{TReturnType}"/>
    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public Task<TReturnType> Resolve(IResolveFieldContext context)
        {
            return _resolver(context.As<TSourceType>());
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
