using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class EventStreamResolver<T> : IEventStreamResolver<T>
    {
        private readonly Func<IResolveEventStreamContext, IObservable<T?>> _subscriber;

        public EventStreamResolver(
            Func<IResolveEventStreamContext, IObservable<T?>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public ValueTask<IObservable<T?>> SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<T?>>(_subscriber(context));

        ValueTask<IObservable<object?>> IEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<object?>>((IObservable<object?>)_subscriber(context));
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver<TReturnType>
    {
        private readonly Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType?>> _subscriber;

        public EventStreamResolver(
            Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType?>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public ValueTask<IObservable<TReturnType?>> SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<TReturnType?>>(_subscriber(context.As<TSourceType>()));

        ValueTask<IObservable<object?>> IEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<object?>>((IObservable<object?>)_subscriber(context.As<TSourceType>()));
    }
}
