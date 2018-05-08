using System;
using System.Threading.Tasks;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    public class AsyncEventStreamResolver<T> : IAsyncEventStreamResolver
    {
        private readonly Func<ResolveEventStreamContext, Task<IObservable<T>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<ResolveEventStreamContext, Task<IObservable<T>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public async Task<IObservable<object>> SubscribeAsync(ResolveEventStreamContext context)
        {
            var result = await _subscriber(context);
            return (IObservable<object>) result;
        }
    }

    public class AsyncEventStreamResolver<TSourceType, TReturnType> : IAsyncEventStreamResolver
    {
        private readonly Func<ResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> _subscriber;

        public AsyncEventStreamResolver(
            Func<ResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public async Task<IObservable<object>> SubscribeAsync(ResolveEventStreamContext context)
        {
            var result = await _subscriber(context.As<TSourceType>());
            return (IObservable<object>) result;
        }
    }
}
