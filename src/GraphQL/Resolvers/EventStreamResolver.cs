#nullable enable

using System;
using GraphQL.Reflection;
using GraphQL.Subscription;
using GraphQL.Utilities;

namespace GraphQL.Resolvers
{
    public class EventStreamResolver<T> : IEventStreamResolver<T>
    {
        private readonly Func<IResolveEventStreamContext, IObservable<T?>> _subscriber;

        public EventStreamResolver(
            Func<IResolveEventStreamContext, IObservable<T?>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<T?> Subscribe(IResolveEventStreamContext context) => _subscriber(context);

        IObservable<object?> IEventStreamResolver.Subscribe(IResolveEventStreamContext context) => (IObservable<object?>)Subscribe(context);
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver<TReturnType>
    {
        private readonly Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType?>> _subscriber;

        public EventStreamResolver(
            Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType?>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<TReturnType?> Subscribe(IResolveEventStreamContext context) => _subscriber(context.As<TSourceType>());

        IObservable<object?> IEventStreamResolver.Subscribe(IResolveEventStreamContext context) => (IObservable<object?>)Subscribe(context);
    }

    public class EventStreamResolver : IEventStreamResolver
    {
        private readonly IAccessor _accessor;
        private readonly IServiceProvider _serviceProvider;

        public EventStreamResolver(IAccessor accessor, IServiceProvider serviceProvider)
        {
            _accessor = accessor;
            _serviceProvider = serviceProvider;
        }

        public IObservable<object?> Subscribe(IResolveEventStreamContext context)
        {
            var parameters = _accessor.Parameters;
            object?[]? arguments = ReflectionHelper.BuildArguments(parameters, context);
            object target = _serviceProvider.GetRequiredService(_accessor.DeclaringType);
            return (IObservable<object?>)_accessor.GetValue(target, arguments)!;
        }

        IObservable<object?> IEventStreamResolver.Subscribe(IResolveEventStreamContext context) => Subscribe(context);
    }
}
