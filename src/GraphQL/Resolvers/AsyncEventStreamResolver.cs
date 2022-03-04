namespace GraphQL.Resolvers
{
    public class AsyncEventStreamResolver<T> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        public AsyncEventStreamResolver(Func<IResolveFieldContext, Task<IObservable<T?>>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(T).IsValueType)
                throw new InvalidOperationException("The generic type T must not be a value type.");

            if (subscriber is Func<IResolveFieldContext, Task<IObservable<object?>>> subscriberObject)
            {
                _subscriber = context => new ValueTask<IObservable<object?>>(subscriberObject(context));
            }
            else
            {
                _subscriber = async context => (IObservable<object?>)await subscriber(context).ConfigureAwait(false);
            }
        }

        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }

    public class AsyncEventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _subscriber;

        public AsyncEventStreamResolver(Func<IResolveFieldContext<TSourceType>, Task<IObservable<TReturnType?>>> subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (typeof(TReturnType).IsValueType)
                throw new InvalidOperationException("The generic type T must not be a value type.");

            if (subscriber is Func<IResolveFieldContext, Task<IObservable<object?>>> subscriberObject)
            {
                _subscriber = context => new ValueTask<IObservable<object?>>(subscriberObject(context.As<TSourceType>()));
            }
            else
            {
                _subscriber = async context => (IObservable<object?>)await subscriber(context.As<TSourceType>()).ConfigureAwait(false);
            }
        }

        public ValueTask<IObservable<object?>> SubscribeAsync(IResolveFieldContext context)
            => _subscriber(context);
    }
}
