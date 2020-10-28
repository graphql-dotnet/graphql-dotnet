using System;
using System.Threading.Tasks;

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

        /// <summary>
        /// Asynchronously returns an object or null for the specified field.
        /// </summary>
        public Task<TReturnType> Resolve(IResolveFieldContext context) => _resolver(context);

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }

    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result.
    /// The returned value must be of an <see cref="Task{TResult}"/> type.
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        /// <summary>
        /// Asynchronously returns an object or null for the specified field.
        /// </summary>
        public Task<TReturnType> Resolve(IResolveFieldContext context) => _resolver(context.As<TSourceType>());

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }
}
