namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a subscription field, this implementation calls a predefined delegate and returns the result.
    /// </summary>
    public class SourceStreamResolver<TReturnType> : ISourceStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _sourceStreamResolver;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a subscription field.
        /// </summary>
        public SourceStreamResolver(Func<IResolveFieldContext, IObservable<TReturnType?>> sourceStreamResolver)
        {
            if (sourceStreamResolver == null)
                throw new ArgumentNullException(nameof(sourceStreamResolver));

            if (typeof(TReturnType).IsValueType)
            {
                _sourceStreamResolver = context => new(new ObservableAdapter<TReturnType?>(sourceStreamResolver(context)));
            }
            else
            {
                _sourceStreamResolver = context => new((IObservable<object?>)sourceStreamResolver(context));
            }
        }

        /// <inheritdoc cref="SourceStreamResolver{TReturnType}(Func{IResolveFieldContext, IObservable{TReturnType}})"/>
        public SourceStreamResolver(Func<IResolveFieldContext, ValueTask<IObservable<TReturnType?>>> sourceStreamResolver)
        {
            if (sourceStreamResolver == null)
                throw new ArgumentNullException(nameof(sourceStreamResolver));

            if (typeof(TReturnType).IsValueType)
            {
                _sourceStreamResolver = async context => new ObservableAdapter<TReturnType?>(await sourceStreamResolver(context).ConfigureAwait(false));
            }
            else
            {
                _sourceStreamResolver = async context => (IObservable<object?>)await sourceStreamResolver(context).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context)
            => _sourceStreamResolver(context);
    }

    /// <inheritdoc cref="SourceStreamResolver{TReturnType}"/>
    public class SourceStreamResolver<TSourceType, TReturnType> : ISourceStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _sourceStreamResolver;

        /// <inheritdoc cref="SourceStreamResolver{TReturnType}(Func{IResolveFieldContext, IObservable{TReturnType}})"/>
        public SourceStreamResolver(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> sourceStreamResolver)
        {
            if (sourceStreamResolver == null)
                throw new ArgumentNullException(nameof(sourceStreamResolver));

            if (typeof(TReturnType).IsValueType)
            {
                _sourceStreamResolver = context => new(new ObservableAdapter<TReturnType?>(sourceStreamResolver(context.As<TSourceType>())));
            }
            else
            {
                _sourceStreamResolver = context => new((IObservable<object?>)sourceStreamResolver(context.As<TSourceType>()));
            }
        }

        /// <inheritdoc cref="SourceStreamResolver{TSourceType, TReturnType}(Func{IResolveFieldContext{TSourceType}, IObservable{TReturnType}})"/>
        /// 
        public SourceStreamResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<IObservable<TReturnType?>>> sourceStreamResolver)
        {
            if (sourceStreamResolver == null)
                throw new ArgumentNullException(nameof(sourceStreamResolver));


            if (typeof(TReturnType).IsValueType)
            {
                _sourceStreamResolver = async context => new ObservableAdapter<TReturnType?>(await sourceStreamResolver(context.As<TSourceType>()).ConfigureAwait(false));
            }
            else
            {
                _sourceStreamResolver = async context => (IObservable<object?>)await sourceStreamResolver(context.As<TSourceType>()).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public ValueTask<IObservable<object?>> ResolveAsync(IResolveFieldContext context)
            => _sourceStreamResolver(context);
    }
}
