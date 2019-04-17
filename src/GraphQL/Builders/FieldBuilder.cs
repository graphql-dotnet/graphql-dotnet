using System;
using GraphQL.Types;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using System.Threading.Tasks;
using GraphQL.Utilities;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(Type type = null)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(IGraphType type)
        {
            return FieldBuilder<TSourceType, TReturnType>.Create(type);
        }
    }

    public class FieldBuilder<TSourceType, TReturnType>
    {
        private readonly EventStreamFieldType _fieldType;

        public EventStreamFieldType FieldType => _fieldType;

        private FieldBuilder(EventStreamFieldType fieldType)
        {
            _fieldType = fieldType;
        }

        public static FieldBuilder<TSourceType, TReturnType> Create(IGraphType type, string name = "default")
        {
            var fieldType = new EventStreamFieldType
            {
                Name = name,
                ResolvedType = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        public static FieldBuilder<TSourceType, TReturnType> Create(Type type = null, string name = "default")
        {
            var fieldType = new EventStreamFieldType
            {
                Name = name,
                Type = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        public FieldBuilder<TSourceType, TReturnType> Type(IGraphType type)
        {
            _fieldType.ResolvedType = type;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Name(string name)
        {
            NameValidator.ValidateName(name);

            _fieldType.Name = name;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Description(string description)
        {
            _fieldType.Description = description;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> DeprecationReason(string deprecationReason)
        {
            _fieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType defaultValue = default)
        {
            _fieldType.DefaultValue = defaultValue;
            return this;
        }

        internal FieldBuilder<TSourceType, TReturnType> DefaultValue(object defaultValue)
        {
            _fieldType.DefaultValue = defaultValue;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver resolver)
        {
            _fieldType.Resolver = resolver;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<ResolveFieldContext<TSourceType>, TReturnType> resolve)
        {
            return Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolve)
        {
            return Resolve(new AsyncFieldResolver<TSourceType, TReturnType>(resolve));
        }

        public FieldBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
        {
            return new FieldBuilder<TSourceType, TNewReturnType>(FieldType);
        }

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string description, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, Action<QueryArgument> configure = null)
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            _fieldType.Arguments.Add(arg);
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Subscribe(Func<ResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscribe)
        {
            FieldType.Subscriber = new EventStreamResolver<TSourceType, TReturnType>(subscribe);
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> SubscribeAsync(Func<ResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> subscribeAsync)
        {
            FieldType.AsyncSubscriber = new AsyncEventStreamResolver<TSourceType, TReturnType>(subscribeAsync);
            return this;
        }
    }
}
