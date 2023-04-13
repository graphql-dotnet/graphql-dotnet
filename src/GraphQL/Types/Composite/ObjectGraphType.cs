using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types.Relay;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an interface for all object (that is, having their own properties) output graph types.
    /// </summary>
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        /// <summary>
        /// Returns a set of fields configured for this graph type.
        /// </summary>
        TypeFields<ObjectFieldType> Fields { get; }

        /// <summary>
        /// Adds a field to this graph type.
        /// </summary>
        ObjectFieldType AddField(ObjectFieldType fieldType);

        /// <summary>
        /// Gets or sets a delegate that determines if the specified object is valid for this graph type.
        /// </summary>
        Func<object, bool>? IsTypeOf { get; set; }

        /// <summary>
        /// Adds an instance of <see cref="IInterfaceGraphType"/> to the list of interface instances supported by this object graph type.
        /// </summary>
        void AddResolvedInterface(IInterfaceGraphType graphType);
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    public class ObjectGraphType : ObjectGraphType<object?>
    {
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    /// <typeparam name="TSourceType">Typically the type of the object that this graph represents. More specifically, the .NET type of the source property within field resolvers for this graph.</typeparam>
    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        /// <inheritdoc />
        public TypeFields<ObjectFieldType> Fields { get; } = new();

        /// <inheritdoc/>
        public virtual ObjectFieldType AddField(ObjectFieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateNameNotNull(fieldType.Name, NamedElement.Field);

            if (!fieldType.ResolvedType.IsGraphQLTypeReference())
            {
                if (fieldType.ResolvedType != null ? fieldType.ResolvedType.IsOutputType() == false : fieldType.Type?.IsOutputType() == false)
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"Object type '{Name ?? GetType().GetFriendlyName()}' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType. Field '{fieldType.Name}' has an input type.");
            }

            if (Fields.Find(fieldType.Name) != null)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType),
                    $"A field with the name '{fieldType.Name}' is already registered for Object '{Name ?? GetType().Name}'");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on Object '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
                }
                else if (!fieldType.Type.IsGraphType())
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared Field type '{fieldType.Type.Name}' should derive from GraphType.");
                }
            }

            Fields.Add(fieldType);

            return fieldType;
        }

        /// <inheritdoc/>
        public Func<object, bool>? IsTypeOf { get; set; }

        /// <inheritdoc/>
        public ObjectGraphType()
        {
            if (typeof(TSourceType) != typeof(object))
                IsTypeOf = instance => instance is TSourceType;
        }

        /// <inheritdoc/>
        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            if (graphType == null)
                throw new ArgumentNullException(nameof(graphType));

            _ = graphType.IsValidInterfaceFor(this, throwError: true);
            ResolvedInterfaces.Add(graphType);
        }

        /// <inheritdoc/>
        public Interfaces Interfaces { get; } = new();

        /// <inheritdoc/>
        public ResolvedInterfaces ResolvedInterfaces { get; } = new();

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Interface<TInterface>()
            where TInterface : IInterfaceGraphType
            => Interfaces.Add<TInterface>();

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Interface(Type type) => Interfaces.Add(type);

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual ObjectFieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return ObjectFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual ObjectFieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>(IGraphType type)
        {
            return ObjectFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by SubscriptionField() methods.
        /// </summary>
        protected virtual SubscriptionRootFieldBuilder<TSourceType, TReturnType> CreateSubscriptionRootBuilder<TReturnType>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return SubscriptionRootFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by SubscriptionField() methods.
        /// </summary>
        protected virtual SubscriptionRootFieldBuilder<TSourceType, TReturnType> CreateSubscriptionRootBuilder<TReturnType>(IGraphType type)
        {
            return SubscriptionRootFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <typeparam name="TReturnType">The return type of the field resolver.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual ObjectFieldBuilder<TSourceType, TReturnType> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType, TReturnType>(string name)
            where TGraphType : IGraphType
        {
            var builder = CreateBuilder<TReturnType>(typeof(TGraphType)).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual ObjectFieldBuilder<TSourceType, object> Field<TGraphType>(string name)
            where TGraphType : IGraphType
            => Field<TGraphType, object>(name);

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field.
        /// </summary>
        public virtual ObjectFieldBuilder<TSourceType, TReturnType> Field<TReturnType>(string name, bool nullable = false)
        {
            Type type;

            try
            {
                type = typeof(TReturnType).GetGraphTypeFromType(nullable, TypeMappingMode.OutputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from type '{typeof(TReturnType).Name}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder<TReturnType>(type)
                .Name(name);

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual ObjectFieldBuilder<TSourceType, object> Field(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual ObjectFieldBuilder<TSourceType, object> Field(string name, IGraphType type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="name">The name of this field.</param>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual ObjectFieldBuilder<TSourceType, TProperty> Field<TProperty>(
            string name,
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
        {
            try
            {
                if (type == null)
                    type = typeof(TProperty).GetGraphTypeFromType(nullable, TypeMappingMode.OutputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from expression '{expression}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder<TProperty>(type)
                .Name(name)
                .Description(expression.DescriptionOf())
                .DeprecationReason(expression.DeprecationReasonOf())
                .Resolve(new ExpressionFieldResolver<TSourceType, TProperty>(expression));

            if (expression.Body is MemberExpression expr)
            {
                builder.FieldType.Metadata[ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
            }

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Object graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// The default name of this field is inferred by the property represented within the expression.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual ObjectFieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
        {
            string name;
            try
            {
                name = expression.NameOf();
            }
            catch
            {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body}' " +
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
