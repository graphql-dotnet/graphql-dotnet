using System;
using System.Linq;
using System.Reflection;
using GraphQL.Reflection;
using GraphQL.Subscription;

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
        private IAccessor _accessor;
        private IDependencyResolver _dependencyResolver;
        private object _target;

        public EventStreamResolver(IAccessor accessor, IDependencyResolver dependencyResolver)
        {
            _accessor = accessor;
            _dependencyResolver = dependencyResolver;
            _target = _dependencyResolver.Resolve(_accessor.DeclaringType);
        }

        public IObservable<object> Subscribe(ResolveEventStreamContext context)
        {
            var parameters = _accessor.Parameters;
            var arguments = ResolverHelper.BuildArguments(parameters, context);
            return (IObservable<object>)_accessor.MethodInfo.Invoke(_target, arguments);
        }

        IObservable<object> IEventStreamResolver.Subscribe(ResolveEventStreamContext context)
        {
            return Subscribe(context);
        }
    }
}
