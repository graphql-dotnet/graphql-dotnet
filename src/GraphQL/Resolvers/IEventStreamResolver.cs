using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public interface IEventStreamResolver
    {
        IObservable<object?> Subscribe(IResolveEventStreamContext context);
    }

    public interface IEventStreamResolver<out T> : IEventStreamResolver
    {
        new IObservable<T?> Subscribe(IResolveEventStreamContext context);
    }

    public interface IAsyncEventStreamResolver
    {
        Task<IObservable<object?>> SubscribeAsync(IResolveEventStreamContext context);
    }

    public interface IAsyncEventStreamResolver<T> : IAsyncEventStreamResolver
    {
        new Task<IObservable<T?>> SubscribeAsync(IResolveEventStreamContext context);
    }
}
