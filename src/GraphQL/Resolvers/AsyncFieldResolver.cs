using System;
using System.Threading.Tasks;
using GraphQL.Execution;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result.
    /// The returned value must be of an <see cref="Task{TResult}"/> type.
    /// </summary>
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<IResolveFieldContext, Task<TReturnType>> _resolver;

        /// <summary>
        /// Initializes a new instance which executes the specified delegate.
        /// </summary>
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
    /// <inheritdoc cref="AsyncFieldResolver{TReturnType}"/>
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<Task<TReturnType>>, IResolveFieldContextProvider
    {
        private readonly Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> _resolver;

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.AsyncFieldResolver(Func{IResolveFieldContext, Task{TReturnType}})"/>
        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        public IResolveFieldContext CreateContext(ExecutionNode node, ExecutionContext context) => new ReadonlyResolveFieldContext<TSourceType>(node, context);

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.Resolve(IResolveFieldContext)"/>
        public Task<TReturnType> Resolve(IResolveFieldContext context)
        {
            return context is IResolveFieldContext<TSourceType> typedContext
               ? _resolver(typedContext)
               : _resolver(new ResolveFieldContext<TSourceType>(context)); //TODO: needed only for tests
               //: throw new ArgumentException($"Context must be of '{typeof(IResolveFieldContext<TSourceType>).Name}' type. Use {typeof(IResolveFieldContextProvider).Name} to create context.", nameof(context));
        }

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }
}
