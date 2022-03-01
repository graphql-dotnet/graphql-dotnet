using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public interface IEventStreamResolver
    {
        ValueTask<IObservable<object?>> SubscribeAsync(IResolveEventStreamContext context);
    }

    public interface IEventStreamResolver<T> : IEventStreamResolver
    {
        new ValueTask<IObservable<T?>> SubscribeAsync(IResolveEventStreamContext context);
    }
}
