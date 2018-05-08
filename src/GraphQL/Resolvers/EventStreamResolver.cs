using System;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class EventStreamResolver<TReturnType> : IEventStreamResolver
    {
        private readonly Func<ResolveEventStreamContext, IObservable<TReturnType>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext, IObservable<TReturnType>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<object> Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>) _subscriber(context);
        }
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver
    {
        private readonly Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<object> Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>) _subscriber(new ResolveEventStreamContext<TSourceType>(context));
        }
    }
}
