using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class AsyncEventStreamResolver<T> : IEventStreamResolver<T>
    {
        private readonly Func<IResolveEventStreamContext, Task<IObservable<T?>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<IResolveEventStreamContext, Task<IObservable<T?>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public ValueTask<IObservable<T?>> SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<T?>>(_subscriber(context));

        async ValueTask<IObservable<object?>> IEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context).ConfigureAwait(false);
            return (IObservable<object?>)result;
        }
    }

    public class AsyncEventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver<TReturnType>
    {
        private readonly Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType?>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType?>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public ValueTask<IObservable<TReturnType?>> SubscribeAsync(IResolveEventStreamContext context)
            => new ValueTask<IObservable<TReturnType?>>(_subscriber(context.As<TSourceType>()));

        async ValueTask<IObservable<object?>> IEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context).ConfigureAwait(false);
            return (IObservable<object?>)result;
        }
    }
}
