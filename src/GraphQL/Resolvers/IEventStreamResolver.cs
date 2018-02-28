using System;
using System.Threading.Tasks;
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

    public interface IAsyncEventStreamResolver
    {
        Task<IObservable<object>> SubscribeAsync(ResolveEventStreamContext context);
    }

    public interface IAsyncEventStreamResolver<T> : IAsyncEventStreamResolver
    {
        new Task<IObservable<T>> SubscribeAsync(ResolveEventStreamContext context);
    }
}
