using System;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class EventStreamResolver<T> : IEventStreamResolver<T>
    {
        private readonly Func<ResolveEventStreamContext, IObservable<T>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext, IObservable<T>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<T> Subscribe(ResolveEventStreamContext context)
        {
            return _subscriber(context);
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>)Subscribe(context);
        }
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver<TReturnType>
    {
        private readonly Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<TReturnType> Subscribe(ResolveEventStreamContext context)
        {
            return _subscriber(context.As<TSourceType>());
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>)Subscribe(context);
        }
    }
}
