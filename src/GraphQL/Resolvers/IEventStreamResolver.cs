using System;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public interface IEventStreamResolver
    {
        IObservable<object> Subscribe(ResolveEventStreamContext context);
    }

    public interface IEventStreamResolver<out T> : IEventStreamResolver
    {
        new IObservable<T> Subscribe(ResolveEventStreamContext context);
    }
}
