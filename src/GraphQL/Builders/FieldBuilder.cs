using System;
using GraphQL.Types;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using System.Threading.Tasks;

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

        public static FieldBuilder<TSourceType, TReturnType> Create(IGraphType type)
        {
            var fieldType = new EventStreamFieldType
            {
                ResolvedType = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        public static FieldBuilder<TSourceType, TReturnType> Create(Type type = null)
        {
            var fieldType = new EventStreamFieldType
            {
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

        public FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType defaultValue = default(TReturnType))
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

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string description)
        {
            _fieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
            });
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default(TArgumentType))
        {
            _fieldType.Arguments.Add(new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
                Description = description,
                DefaultValue = defaultValue,
            });
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
