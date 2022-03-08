namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a subscription field, this implementation calls a predefined delegate and returns the result.
    /// </summary>
    public class EventStreamResolver<TReturnType> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a subscription field.
        /// </summary>
        public EventStreamResolver(Func<IResolveFieldContext, IObservable<TReturnType?>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = context => new ValueTask<IObservable<object?>>((IObservable<object?>)subscriber(context));
        }

        /// <inheritdoc cref="EventStreamResolver{TReturnType}.EventStreamResolver(Func{IResolveFieldContext, IObservable{TReturnType}})"/>
        public EventStreamResolver(Func<IResolveFieldContext, ValueTask<IObservable<TReturnType?>>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = async context => (IObservable<object?>)await subscriber(context).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }

    /// <inheritdoc cref="EventStreamResolver{TReturnType}"/>
    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        /// <inheritdoc cref="EventStreamResolver{TReturnType}.EventStreamResolver(Func{IResolveFieldContext, IObservable{TReturnType}})"/>
        public EventStreamResolver(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = context => new ValueTask<IObservable<object?>>((IObservable<object?>)subscriber(context.As<TSourceType>()));
        }

        /// <inheritdoc cref="EventStreamResolver{TSourceType, TReturnType}.EventStreamResolver(Func{IResolveFieldContext{TSourceType}, IObservable{TReturnType}})"/>
        public EventStreamResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<IObservable<TReturnType?>>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = async context => (IObservable<object?>)await subscriber(context.As<TSourceType>()).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }
}
