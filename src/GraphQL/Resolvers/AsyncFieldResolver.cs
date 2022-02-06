namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result.
    /// The returned value must be of an <see cref="Task{TResult}"/> type.
    /// </summary>
    public class AsyncFieldResolver<TReturnType> : IFieldResolver<TReturnType?>
    {
        private readonly Func<IResolveFieldContext, ValueTask<TReturnType?>> _resolver;

        /// <summary>
        /// Initializes a new instance which executes the specified delegate.
        /// </summary>
        public AsyncFieldResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.AsyncFieldResolver(Func{IResolveFieldContext, ValueTask{TReturnType?}})" />
        public AsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType?>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _resolver = context => new ValueTask<TReturnType?>(resolver(context));
        }

        /// <summary>
        /// Asynchronously returns an object or <see langword="null"/> for the specified field.
        /// </summary>
        public ValueTask<TReturnType?> ResolveAsync(IResolveFieldContext context) => _resolver(context);

        async ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => await ResolveAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// <inheritdoc cref="AsyncFieldResolver{TReturnType}"/>
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class AsyncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType?>
    {
        private readonly Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> _resolver;

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.AsyncFieldResolver(Func{IResolveFieldContext, ValueTask{TReturnType}})"/>
        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
        }

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.AsyncFieldResolver(Func{IResolveFieldContext, Task{TReturnType}})"/>
        public AsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");

            _resolver = context => new ValueTask<TReturnType?>(resolver(context));
        }

        /// <inheritdoc cref="AsyncFieldResolver{TReturnType}.ResolveAsync(IResolveFieldContext)"/>
        public ValueTask<TReturnType?> ResolveAsync(IResolveFieldContext context) => _resolver(context.As<TSourceType>());

        async ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => await ResolveAsync(context).ConfigureAwait(false);
    }
}
