using System;
using GraphQL.Reflection;
using GraphQL.Subscription;
using GraphQL.Utilities;

namespace GraphQL.Resolvers
{
    public class EventStreamResolver<T> : IEventStreamResolver<T>
    {
        private readonly Func<ResolveEventStreamContext, IObservable<T>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext, IObservable<T>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<T> Subscribe(ResolveEventStreamContext context)
        {
            return _subscriber(context);
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>)Subscribe(context);
        }
    }

    public class EventStreamResolver<TSourceType, TReturnType> : IEventStreamResolver<TReturnType>
    {
        private readonly Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> _subscriber;

        public EventStreamResolver(
            Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public IObservable<TReturnType> Subscribe(ResolveEventStreamContext context)
        {
            return _subscriber(context.As<TSourceType>());
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return (IObservable<object>)Subscribe(context);
        }
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

        public IObservable<object> Subscribe(ResolveEventStreamContext context)
        {
            var parameters = _accessor.Parameters;
            var arguments = ReflectionHelper.BuildArguments(parameters, context);
            var target = _serviceProvider.GetRequiredService(_accessor.DeclaringType);
            return (IObservable<object>)_accessor.GetValue(target, arguments);
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return Subscribe(context);
        }
    }
}
