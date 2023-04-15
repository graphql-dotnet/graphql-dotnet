using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an input object graph type.
    /// </summary>
    public interface IInputObjectGraphType : IComplexGraphType
    {
        /// <summary>
        /// Returns a set of fields configured for this graph type.
        /// </summary>
        TypeFields<InputFieldType> Fields { get; }

        /// <summary>
        /// Adds a field to this graph type.
        /// </summary>
        InputFieldType AddField(InputFieldType fieldType);

        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        object ParseDictionary(IDictionary<string, object?> value);

        /// <summary>
        /// Returns a boolean indicating if the provided value is valid as a default value for a
        /// field or argument of this type.
        /// </summary>
        bool IsValidDefault(object value);

        /// <summary>
        /// Converts a value to an AST representation. This is necessary for introspection queries
        /// to return the default value for fields of this scalar type. This method may throw an exception
        /// or return <see langword="null"/> for a failed conversion.
        /// </summary>
        GraphQLValue ToAST(object value);
    }

    /// <inheritdoc/>
    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    /// <inheritdoc cref="IInputObjectGraphType"/>
    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
        /// <inheritdoc />
        public TypeFields<InputFieldType> Fields { get; } = new();

        /// <inheritdoc/>
        public virtual InputFieldType AddField(InputFieldType fieldType)
        {
            if (fieldType == null)
                throw new ArgumentNullException(nameof(fieldType));

            NameValidator.ValidateNameNotNull(fieldType.Name, NamedElement.Field);

            if (!fieldType.ResolvedType.IsGraphQLTypeReference())
            {
                if (fieldType.ResolvedType != null ? fieldType.ResolvedType.IsInputType() == false : fieldType.Type?.IsInputType() == false)
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"Input Object '{Name ?? GetType().GetFriendlyName()}' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType. Field '{fieldType.Name}' has an output type.");
            }

            if (Fields.Find(fieldType.Name) != null)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType),
                    $"A field with the name '{fieldType.Name}' is already registered for Input Object '{Name ?? GetType().Name}'");
            }

            if (fieldType.ResolvedType == null)
            {
                if (fieldType.Type == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(fieldType),
                        $"The declared field '{fieldType.Name ?? fieldType.GetType().GetFriendlyName()}' on Input Object '{Name ?? GetType().GetFriendlyName()}' requires a field '{nameof(fieldType.Type)}' when no '{nameof(fieldType.ResolvedType)}' is provided.");
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

        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// The default implementation uses <see cref="ObjectExtensions.ToObject"/> to convert the
        /// supplied field values into an object of type <typeparamref name="TSourceType"/>.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        public virtual object ParseDictionary(IDictionary<string, object?> value)
        {
            if (value == null)
                return null!;

            // for InputObjectGraphType just return the dictionary
            if (typeof(TSourceType) == typeof(object))
                return value;

            // for InputObjectGraphType<TSourceType>, convert to TSourceType via ToObject.
            return value.ToObject(typeof(TSourceType), this);
        }

        /// <inheritdoc/>
        public virtual bool IsValidDefault(object value)
        {
            if (value is not TSourceType)
                return false;

            foreach (var field in Fields)
            {
                if (!field.ResolvedType!.IsValidDefault(GetFieldValue(field, value)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a value to an AST representation. This is necessary for introspection queries
        /// to return the default value for fields of this input object type. Also AST representation
        /// is used while printing schema as SDL.
        /// <br/>
        /// This method may throw an exception or return <see langword="null"/> for a failed conversion.
        /// <br/><br/>
        /// The default implementation returns <see cref="GraphQLNullValue"/> if <paramref name="value"/>
        /// is <see langword="null"/> and <see cref="GraphQLObjectValue"/> filled with the values
        /// for all input fields except ones returning <see cref="GraphQLNullValue"/>.
        /// <br/><br/>
        /// Note that you may need to override this method if you have already overrided <see cref="ParseDictionary"/>.
        /// </summary>
        public virtual GraphQLValue ToAST(object? value)
        {
            if (value == null)
                return GraphQLValuesCache.Null;

            var objectValue = new GraphQLObjectValue
            {
                Fields = new List<GraphQLObjectField>(Fields.Count)
            };

            foreach (var field in Fields)
            {
                var fieldValue = field.ResolvedType!.ToAST(GetFieldValue(field, value));
                if (fieldValue is not GraphQLNullValue)
                {
                    objectValue.Fields.Add(new GraphQLObjectField
                    {
                        Name = new GraphQLName(field.Name),
                        Value = fieldValue
                    });
                }
            }

            return objectValue;
        }

        private static object? GetFieldValue(FieldType field, object? value)
        {
            if (value == null)
                return null;

            // Given Field(x => x.FName).Name("FirstName") and key == "FirstName" returns "FName"
            string propertyName = field.GetMetadata(ObjectExtensions.ORIGINAL_EXPRESSION_PROPERTY_NAME, field.Name) ?? field.Name;
            PropertyInfo? propertyInfo;
            try
            {
                propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
            catch (AmbiguousMatchException)
            {
                propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }

            return propertyInfo?.CanRead == true
                ? propertyInfo.GetValue(value)
                : null;
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual InputFieldBuilder CreateBuilder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return InputFieldBuilder.Create(type);
        }

        /// <summary>
        /// Creates a field builder used by Field() methods.
        /// </summary>
        protected virtual InputFieldBuilder CreateBuilder(IGraphType type)
        {
            return InputFieldBuilder.Create(type);
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <typeparam name="TGraphType">The .NET type of the graph type of this field.</typeparam>
        /// <param name="name">The name of the field.</param>
        public virtual InputFieldBuilder Field<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TGraphType>(string name)
            where TGraphType : IGraphType
        {
            var builder = CreateBuilder(typeof(TGraphType)).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field.
        /// </summary>
        public virtual InputFieldBuilder Field<TReturnType>(string name, bool nullable = false)
        {
            Type type;

            try
            {
                type = typeof(TReturnType).GetGraphTypeFromType(nullable, TypeMappingMode.InputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from type '{typeof(TReturnType).Name}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder(type)
                .Name(name);

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The .NET type of the graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual InputFieldBuilder Field(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            var builder = CreateBuilder(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field.
        /// </summary>
        /// <param name="type">The graph type of this field.</param>
        /// <param name="name">The name of the field.</param>
        public virtual InputFieldBuilder Field(string name, IGraphType type)
        {
            var builder = CreateBuilder(type).Name(name);
            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="name">The name of this field.</param>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual InputFieldBuilder Field<TProperty>(
            string name,
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
        {
            try
            {
                if (type == null)
                    type = typeof(TProperty).GetGraphTypeFromType(nullable, TypeMappingMode.InputType);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException($"The GraphQL type for field '{Name ?? GetType().Name}.{name}' could not be derived implicitly from expression '{expression}'. " + exp.Message, exp);
            }

            var builder = CreateBuilder(type)
                .Name(name)
                .Description(expression.DescriptionOf())
                .DeprecationReason(expression.DeprecationReasonOf())
                .DefaultValue(expression.DefaultValueOf());

            if (expression.Body is MemberExpression expr)
            {
                builder.FieldType.Metadata[ObjectExtensions.ORIGINAL_EXPRESSION_PROPERTY_NAME] = expr.Member.Name;
            }

            AddField(builder.FieldType);
            return builder;
        }

        /// <summary>
        /// Adds a new field to the Input Object graph type and returns a builder for this newly added field that is linked to a property of the source object.
        /// The default name of this field is inferred by the property represented within the expression.
        /// <br/><br/>
        /// Note: this method uses dynamic compilation and therefore allocates a relatively large amount of
        /// memory in managed heap, ~1KB. Do not use this method in cases with limited memory requirements.
        /// </summary>
        /// <typeparam name="TProperty">The return type of the field.</typeparam>
        /// <param name="expression">The property of the source object represented within an expression.</param>
        /// <param name="nullable">Indicates if this field should be nullable or not. Ignored when <paramref name="type"/> is specified.</param>
        /// <param name="type">The graph type of the field; if <see langword="null"/> then will be inferred from the specified expression via registered schema mappings.</param>
        public virtual InputFieldBuilder Field<TProperty>(
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
    }
}
