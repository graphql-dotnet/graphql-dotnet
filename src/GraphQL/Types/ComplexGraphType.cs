using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GraphQL.Types
{
    public interface IComplexGraphType : IGraphType
    {
        IEnumerable<FieldType> Fields { get; }

        FieldType AddField(FieldType fieldType);

        bool HasField(string name);

        FieldType GetField(string name);
    }

    public abstract class ComplexGraphType<TSourceType> : GraphType, IComplexGraphType
    {
        private readonly List<FieldType> _fields = new List<FieldType>();

        protected ComplexGraphType()
        {
            Description = Description ?? typeof(TSourceType).Description();
            DeprecationReason = DeprecationReason ?? typeof(TSourceType).ObsoleteMessage();
        }

        public IEnumerable<FieldType> Fields
        {
            get => _fields;
            private set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }

        public bool HasField(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            return _fields.Any(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        }

        public FieldType GetField(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            return _fields.Find(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        }

        public virtual FieldType AddField(FieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateName(fieldType.Name);

            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Name),
                    $"A field with the name: {fieldType.Name} is already registered for GraphType: {Name ?? GetType().Name}");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType.Type),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
                }
                else if (!fieldType.Type.IsGraphType())
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType.Type),
                        $"The declared Field type: {fieldType.Type.Name} should derive from GraphType.");
                }
            }

            _fields.Add(fieldType);

            return fieldType;
        }

        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        public FieldType Field<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        public FieldType FieldDelegate<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Delegate resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new DelegateFieldModelBinderResolver(resolve)
                    : null
            });
        }

        public FieldType FieldAsync(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, Task<object>> resolve = null,
            string deprecationReason = null)
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolver = resolve != null
                    ? new AsyncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        public FieldType FieldAsync<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, Task<object>> resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new AsyncFieldResolver<TSourceType, object>(resolve)
                    : null
            });
        }

        public FieldType FieldAsync<TGraphType, TReturnType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, Task<TReturnType>> resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new AsyncFieldResolver<TSourceType, TReturnType>(resolve)
                    : null
            });
        }

        public FieldType FieldSubscribe<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            Func<ResolveEventStreamContext, IObservable<object>> subscribe = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new EventStreamFieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
                Subscriber = subscribe != null
                    ? new EventStreamResolver<object>(subscribe)
                    : null
            });
        }

        public FieldType FieldSubscribeAsync<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            Func<ResolveEventStreamContext, Task<IObservable<object>>> subscribeAsync = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new EventStreamFieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
                AsyncSubscriber = subscribeAsync != null
                    ? new AsyncEventStreamResolver<object>(subscribeAsync)
                    : null
            });
        }

        public FieldBuilder<TSourceType, TReturnType> Field<TGraphType, TReturnType>(string name = "default")
        {
            var builder = FieldBuilder.Create<TSourceType, TReturnType>(typeof(TGraphType))
                .Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, object> Field<TGraphType>()
            => Field<TGraphType, object>();

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
           string name,
           Expression<Func<TSourceType, TProperty>> expression,
           bool nullable = false,
           Type type = null)
        {
            try
            {
                if (type == null)
                    type = typeof(TProperty).GetGraphTypeFromType(nullable);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException(
                    $"The GraphQL type for Field: '{name}' on parent type: '{Name ?? GetType().Name}' could not be derived implicitly. \n",
                    exp
                 );
            }

            var builder = FieldBuilder.Create<TSourceType, TProperty>(type)
                .Name(name)
                .Resolve(new ExpressionFieldResolver<TSourceType, TProperty>(expression))
                .Description(expression.DescriptionOf())
                .DeprecationReason(expression.DeprecationReasonOf())
                .DefaultValue(expression.DefaultValueOf());

            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            Type type = null)
        {
            string name;
            try {
                name = expression.NameOf();
            }
            catch {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body.ToString()}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
            return Field(name, expression, nullable, type);
        }

        public ConnectionBuilder<TSourceType> Connection<TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        public ConnectionBuilder<TSourceType> Connection<TNodeType, TEdgeType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
        {
            var builder = ConnectionBuilder.Create<TNodeType, TEdgeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        public ConnectionBuilder<TSourceType> Connection<TNodeType, TEdgeType, TConnectionType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
            where TConnectionType : ConnectionType<TNodeType, TEdgeType>
        {
            var builder = ConnectionBuilder.Create<TNodeType, TEdgeType, TConnectionType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }
    }
}
