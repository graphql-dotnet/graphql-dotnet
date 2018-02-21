using System;
using System.Threading.Tasks;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class AsyncEventStreamResolver<T> : IAsyncEventStreamResolver<T>
    {
        private readonly Func<ResolveEventStreamContext, Task<IObservable<T>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<ResolveEventStreamContext, Task<IObservable<T>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public Task<IObservable<T>> SubscribeAsync(ResolveEventStreamContext context)
        {
            return _subscriber(context);
        }

        async Task<IObservable<object>> IAsyncEventStreamResolver.SubscribeAsync(ResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context);
            return (IObservable<object>)result;
        }
    }

    public class AsyncEventStreamResolver<TSourceType, TReturnType> : IAsyncEventStreamResolver<TReturnType>
    {
        private readonly Func<ResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<ResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public Task<IObservable<TReturnType>> SubscribeAsync(ResolveEventStreamContext context)
        {
            return _subscriber(context.As<TSourceType>());
        }

        async Task<IObservable<object>> IAsyncEventStreamResolver.SubscribeAsync(ResolveEventStreamContext context)
        {
            var result = await SubscribeAsync(context);
            return (IObservable<object>)result;
        }
    }
}
