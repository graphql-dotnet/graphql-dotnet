using System;
using System.Threading.Tasks;
using GraphQL.Reflection;
using GraphQL.Subscription;
using GraphQL.Utilities;

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

    public class AsyncEventStreamResolver : IAsyncEventStreamResolver
    {
        private readonly IAccessor _accessor;
        private readonly IServiceProvider _serviceProvider;

        public AsyncEventStreamResolver(IAccessor accessor, IServiceProvider serviceProvider)
        {
            _accessor = accessor;
            _serviceProvider = serviceProvider;
        }

        async Task<IObservable<object>> IAsyncEventStreamResolver.SubscribeAsync(ResolveEventStreamContext context)
        {
            var parameters = _accessor.Parameters;
            var arguments = ReflectionHelper.BuildArguments(parameters, context);
            var target = _serviceProvider.GetRequiredService(_accessor.DeclaringType);
            var result = _accessor.GetValue(target, arguments);

            if (!(result is Task task))
            {
                throw new ArgumentException($"Return type of {_accessor.FieldName} should be Task<IObservable<T>>, instead of {_accessor.ReturnType}");
            }

            await task;

            return ((dynamic)task).Result;
        }
    }
}
