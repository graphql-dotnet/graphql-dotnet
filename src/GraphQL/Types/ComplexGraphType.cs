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
            Description ??= typeof(TSourceType).Description();
            DeprecationReason ??= typeof(TSourceType).ObsoleteMessage();
        }

        public IEnumerable<FieldType> Fields => _fields;

        public bool HasField(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            return _fields.Any(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        }

        public FieldType GetField(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var x in _fields)
                if (string.Equals(x.Name, name, StringComparison.Ordinal))
                    return x;

            return null;
        }

        public virtual FieldType AddField(FieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            if (!(fieldType.ResolvedType.GetNamedType() is GraphQLTypeReference))
            {
                if (this is IInputObjectGraphType)
                {
                    if (fieldType.ResolvedType?.IsInputType() == false || fieldType.Type?.IsInputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Input type '{Name ?? GetType().GetFriendlyName()}' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType.");
                }
                else
                {
                    if (fieldType.ResolvedType?.IsOutputType() == false || fieldType.Type?.IsOutputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Output type '{Name ?? GetType().GetFriendlyName()}' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType.");
                }
            }

            NameValidator.ValidateName(fieldType.Name);

            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType),
                    $"A field with the name '{fieldType.Name}' is already registered for GraphType '{Name ?? GetType().Name}'");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
                }
                else if (!fieldType.Type.IsGraphType())
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared Field type '{fieldType.Type.Name}' should derive from GraphType.");
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

        public virtual FieldBuilder<TSourceType, TReturnType> Field<TGraphType, TReturnType>(string name = "default")
        {
            var builder = FieldBuilder.Create<TSourceType, TReturnType>(typeof(TGraphType))
                .Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        public virtual FieldBuilder<TSourceType, object> Field<TGraphType>()
            => Field<TGraphType, object>();

        public virtual FieldBuilder<TSourceType, TProperty> Field<TProperty>(
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

        public virtual FieldBuilder<TSourceType, TProperty> Field<TProperty>(
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
