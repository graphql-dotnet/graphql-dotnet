namespace GraphQL.Resolvers
{
    public class EventStreamResolver<TReturnType> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        public EventStreamResolver(Func<IResolveFieldContext, IObservable<TReturnType?>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = context => new ValueTask<IObservable<object?>>((IObservable<object?>)subscriber(context));
        }

        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        public EventStreamResolver(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type TReturnType must not be a value type.");

            _subscriber = context => new ValueTask<IObservable<object?>>((IObservable<object?>)subscriber(context.As<TSourceType>()));
        }

        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }
}
