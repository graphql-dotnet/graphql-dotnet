using GraphQL.Reflection;
using GraphQL.Subscription;
using GraphQL.Utilities;

namespace GraphQL.Resolvers
{
    public class AsyncEventStreamResolver<T> : IAsyncEventStreamResolver<T>
    {
        private readonly Func<IResolveEventStreamContext, Task<IObservable<T?>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<IResolveEventStreamContext, Task<IObservable<T?>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public Task<IObservable<T?>> SubscribeAsync(IResolveEventStreamContext context) => _subscriber(context);

        async Task<IObservable<object?>> IAsyncEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context).ConfigureAwait(false);
            return (IObservable<object?>)result;
        }
    }

    public class AsyncEventStreamResolver<TSourceType, TReturnType> : IAsyncEventStreamResolver<TReturnType>
    {
        private readonly Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType?>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType?>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public Task<IObservable<TReturnType?>> SubscribeAsync(IResolveEventStreamContext context) => _subscriber(context.As<TSourceType>());

        async Task<IObservable<object?>> IAsyncEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context).ConfigureAwait(false);
            return (IObservable<object?>)result;
        }
    }
}
