using System;
using GraphQL.Execution;
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
}
