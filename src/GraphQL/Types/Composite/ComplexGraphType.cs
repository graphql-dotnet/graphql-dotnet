using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types.Relay;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an interface for all complex (that is, having their own properties) input and output graph types.
    /// </summary>
    public interface IComplexGraphType : IGraphType
    {
        /// <summary>
        /// Returns a list of the fields configured for this graph type.
        /// </summary>
        IEnumerable<FieldType> Fields { get; }

        /// <summary>
        /// Adds a field to this graph type.
        /// </summary>
        FieldType AddField(FieldType fieldType);

        /// <summary>
        /// Returns <see langword="true"/> when a field matching the specified name is configured for this graph type.
        /// </summary>
        bool HasField(string name);

        /// <summary>
        /// Returns the <see cref="FieldType"/> for the field matching the specified name that
        /// is configured for this graph type, or <see langword="null"/> if none is found.
        /// </summary>
        FieldType GetField(string name);
    }

    /// <summary>
    /// Represents a default base class for all complex (that is, having their own properties) input and output graph types.
    /// </summary>
    public abstract class ComplexGraphType<TSourceType> : GraphType, IComplexGraphType
    {
        internal const string ORIGINAL_EXPRESSION_PROPERTY_NAME = nameof(ORIGINAL_EXPRESSION_PROPERTY_NAME);
        private readonly List<FieldType> _fields = new List<FieldType>();

        /// <inheritdoc/>
        protected ComplexGraphType()
        {
            Description ??= typeof(TSourceType).Description();
            DeprecationReason ??= typeof(TSourceType).ObsoleteMessage();
        }

        /// <inheritdoc/>
        public IEnumerable<FieldType> Fields => _fields;

        /// <inheritdoc/>
        public bool HasField(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return _fields.Any(x => string.Equals(x.Name, name, StringComparison.Ordinal));
        }

        /// <inheritdoc/>
        public FieldType GetField(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var x in _fields)
                if (string.Equals(x.Name, name, StringComparison.Ordinal))
                    return x;

            return null;
        }

        /// <inheritdoc/>
        public virtual FieldType AddField(FieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateNameNotNull(fieldType.Name);

            if (!(fieldType.ResolvedType.GetNamedType() is GraphQLTypeReference))
            {
                if (this is IInputObjectGraphType)
                {
                    if (fieldType.ResolvedType?.IsInputType() == false || fieldType.Type?.IsInputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Input type '{Name ?? GetType().GetFriendlyName()}' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType. Field '{fieldType.Name}' has an output type.");
                }
                else
                {
                    if (fieldType.ResolvedType?.IsOutputType() == false || fieldType.Type?.IsOutputType() == false)
                        throw new ArgumentOutOfRangeException(nameof(fieldType),
                            $"Output type '{Name ?? GetType().GetFriendlyName()}' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType. Field '{fieldType.Name}' has an input type.");
                }
            }

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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<IResolveFieldContext<TSourceType>, object> resolve = null,
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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        public FieldType Field<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<IResolveFieldContext<TSourceType>, object> resolve = null,
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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        public FieldType FieldAsync(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<object>> resolve = null,
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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        public FieldType FieldAsync<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<object>> resolve = null,
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

        /// <summary>
        /// Adds a field with the specified properties to this graph type.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <typeparam name="TReturnType">The type of the return value of the field resolver delegate.</typeparam>
        /// <param name="name">The name of the field.</param>
        /// <param name="description">The description of the field.</param>
        /// <param name="arguments">A list of arguments for the field.</param>
        /// <param name="resolve">A field resolver delegate. Only applicable to fields of output graph types. If not specified, <see cref="NameFieldResolver"/> will be used.</param>
        /// <param name="deprecationReason">The deprecation reason for the field. Applicable only for output graph types.</param>
        /// <returns>The newly added <see cref="FieldType"/> instance.</returns>
        public FieldType FieldAsync<TGraphType, TReturnType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolve = null,
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
            Func<IResolveFieldContext<TSourceType>, object> resolve = null,
            Func<IResolveEventStreamContext, IObservable<object>> subscribe = null,
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
            Func<IResolveFieldContext<TSourceType>, object> resolve = null,
            Func<IResolveEventStreamContext, Task<IObservable<object>>> subscribeAsync = null,
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

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The graph type of the field.</typeparam>
        /// <typeparam name="TReturnType">The return type of the field resolver.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Field<TGraphType, TReturnType>(string name = "default")
        {
            var builder = FieldBuilder.Create<TSourceType, TReturnType>(typeof(TGraphType))
                .Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The graph type of the field.</typeparam>
        public virtual FieldBuilder<TSourceType, object> Field<TGraphType>() => Field<TGraphType, object>();

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="name">The name of this field.</param>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; inferred via <see cref="GraphTypeTypeRegistry"/> if null.</param>
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

            if (expression.Body is MemberExpression expr)
            {
                builder.FieldType.Metadata[ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
            }

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the complex graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// The default name of this field is inferred by the property represented within the expression.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; inferred via <see cref="GraphTypeTypeRegistry"/> if null.</param>
        public virtual FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            Type type = null)
        {
            string name;
            try
            {
                name = expression.NameOf();
            }
            catch
            {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body.ToString()}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
            return Field(name, expression, nullable, type);
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType}(string)"/>
        public ConnectionBuilder<TSourceType> Connection<TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType, TEdgeType}(string)"/>
        public ConnectionBuilder<TSourceType> Connection<TNodeType, TEdgeType>()
            where TNodeType : IGraphType
            where TEdgeType : EdgeType<TNodeType>
        {
            var builder = ConnectionBuilder.Create<TNodeType, TEdgeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }

        /// <inheritdoc cref="ConnectionBuilder{TSourceType}.Create{TNodeType, TEdgeType, TConnectionType}(string)"/>
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
