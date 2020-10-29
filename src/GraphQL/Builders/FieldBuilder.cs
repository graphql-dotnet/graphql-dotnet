using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Builders
{
    public static class FieldBuilder
    {
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(Type type = null)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);

        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(IGraphType type)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);
    }

    public class FieldBuilder<TSourceType, TReturnType>
    {
        public EventStreamFieldType FieldType { get; }

        private FieldBuilder(EventStreamFieldType fieldType)
        {
            FieldType = fieldType;
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

        public virtual FieldBuilder<TSourceType, TReturnType> Type(IGraphType type)
        {
            FieldType.ResolvedType = type;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType defaultValue = default)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        internal FieldBuilder<TSourceType, TReturnType> DefaultValue(object defaultValue)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver resolver)
        {
            FieldType.Resolver = resolver;
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));

        public virtual FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolve)
            => Resolve(new AsyncFieldResolver<TSourceType, TReturnType>(resolve));

        public virtual FieldBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
            => new FieldBuilder<TSourceType, TNewReturnType>(FieldType);

        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string description, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, Action<QueryArgument> configure = null)
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments.Add(arg);
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> Subscribe(Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscribe)
        {
            FieldType.Subscriber = new EventStreamResolver<TSourceType, TReturnType>(subscribe);
            return this;
        }

        public virtual FieldBuilder<TSourceType, TReturnType> SubscribeAsync(Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> subscribeAsync)
        {
            FieldType.AsyncSubscriber = new AsyncEventStreamResolver<TSourceType, TReturnType>(subscribeAsync);
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Directive(SchemaDirectiveVisitor directive)
        {
            FieldType.AddDirective(directive);
            return this;
        }
    }
}
