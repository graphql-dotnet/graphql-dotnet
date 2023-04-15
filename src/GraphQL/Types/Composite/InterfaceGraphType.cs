using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a GraphQL interface graph type.
    /// </summary>
    public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType
    {
        /// <summary>
        /// Returns a set of fields configured for this graph type.
        /// </summary>
        TypeFields<InterfaceFieldType> Fields { get; }

        /// <summary>
        /// Adds a field to this graph type.
        /// </summary>
        InterfaceFieldType AddField(InterfaceFieldType fieldType);
    }

    /// <inheritdoc cref="IInterfaceGraphType"/>
    public class InterfaceGraphType : InterfaceGraphType<object>
    {
    }

    /// <inheritdoc cref="InterfaceGraphType"/>
    public class InterfaceGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInterfaceGraphType
    {
        /// <inheritdoc />
        public TypeFields<InterfaceFieldType> Fields { get; } = new();

        /// <inheritdoc/>
        public virtual InterfaceFieldType AddField(InterfaceFieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateNameNotNull(fieldType.Name, NamedElement.Field);

            if (!fieldType.ResolvedType.IsGraphQLTypeReference())
            {
                if (fieldType.ResolvedType != null ? fieldType.ResolvedType.IsOutputType() == false : fieldType.Type?.IsOutputType() == false)
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"Interface type '{Name ?? GetType().GetFriendlyName()}' can have fields only of output types: ScalarGraphType, ObjectGraphType, InterfaceGraphType, UnionGraphType or EnumerationGraphType. Field '{fieldType.Name}' has an input type.");
            }

            if (Fields.Find(fieldType.Name) != null)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType),
                    $"A field with the name '{fieldType.Name}' is already registered for Interface '{Name ?? GetType().Name}'");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on Interface '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
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
        public PossibleTypes PossibleTypes { get; } = new();

        /// <inheritdoc/>
        public Func<object, IObjectGraphType?>? ResolveType { get; set; }

        /// <inheritdoc/>
        public void AddPossibleType(IObjectGraphType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            this.IsValidInterfaceFor(type, throwError: true);
            PossibleTypes.Add(type);
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual InterfaceFieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return InterfaceFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual InterfaceFieldBuilder<TSourceType, TReturnType> CreateBuilder<TReturnType>(IGraphType type)
        {
            return InterfaceFieldBuilder<TSourceType, TReturnType>.Create(type);
        }

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <typeparam name="TReturnType">The return type of the field resolver.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual InterfaceFieldBuilder<TSourceType, TReturnType> Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType, TReturnType>(string name)
            where TGraphType : IGraphType
        {
            var builder = CreateBuilder<TReturnType>(typeof(TGraphType)).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual InterfaceFieldBuilder<TSourceType, object> Field<TGraphType>(string name)
            where TGraphType : IGraphType
            => Field<TGraphType, object>(name);

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field.
        /// </summary>
        public virtual InterfaceFieldBuilder<TSourceType, TReturnType> Field<TReturnType>(string name, bool nullable = false)
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
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual InterfaceFieldBuilder<TSourceType, object> Field(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual InterfaceFieldBuilder<TSourceType, object> Field(string name, IGraphType type)
        {
            var builder = CreateBuilder<object>(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="name">The name of this field.</param>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual InterfaceFieldBuilder<TSourceType, TProperty> Field<TProperty>(
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
                .DeprecationReason(expression.DeprecationReasonOf());

            if (expression.Body is MemberExpression expr)
            {
                builder.FieldType.Metadata[ObjectExtensions.ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
            }

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Interface graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// The default name of this field is inferred by the property represented within the expression.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual InterfaceFieldBuilder<TSourceType, TProperty> Field<TProperty>(
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

        /*
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
        */
    }
}
