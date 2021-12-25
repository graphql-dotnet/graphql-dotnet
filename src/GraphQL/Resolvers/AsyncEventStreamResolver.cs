using System;
using System.Threading.Tasks;
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

    public class AsyncEventStreamResolver : IAsyncEventStreamResolver
    {
        private readonly IAccessor _accessor;
        private readonly IServiceProvider _serviceProvider;

        public AsyncEventStreamResolver(IAccessor accessor, IServiceProvider serviceProvider)
        {
            _accessor = accessor;
            _serviceProvider = serviceProvider;
        }

        async Task<IObservable<object?>> IAsyncEventStreamResolver.SubscribeAsync(IResolveEventStreamContext context)
        {
            var parameters = _accessor.Parameters;
            var arguments = ReflectionHelper.BuildArguments(parameters, context);
            var target = _serviceProvider.GetRequiredService(_accessor.DeclaringType);
            var result = _accessor.GetValue(target, arguments);

            if (result is not Task task)
            {
                throw new ArgumentException($"Return type of {_accessor.FieldName} should be Task<IObservable<T>>, instead of {_accessor.ReturnType}");
            }

            await task.ConfigureAwait(false);

            return ((dynamic)task).Result;
        }
    }
}
