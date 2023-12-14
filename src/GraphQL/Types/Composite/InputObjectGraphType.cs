using System.Reflection;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an input object graph type.
    /// </summary>
    public interface IInputObjectGraphType : IComplexGraphType
    {
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
    public class InputObjectGraphType<[NotAGraphType][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
        private Func<IDictionary<string, object?>, object>? _parseDictionary;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public InputObjectGraphType()
            : this(null)
        {
        }

        internal InputObjectGraphType(InputObjectGraphType<TSourceType>? cloneFrom)
            : base(cloneFrom)
        {
            // if (cloneFrom == null) { /* initialization logic */ }

            if (typeof(TSourceType) == typeof(object))
            {
                // for InputObjectGraphType just return the dictionary
                _parseDictionary = static x => x;
            }
        }

        /// <inheritdoc/>
        public override void Initialize(ISchema schema)
        {
            base.Initialize(schema);

            if (_parseDictionary == null) // when typeof(TSourceType) != typeof(object)
            {
                // check the value converter for a conversion from dictionary to this object type
                var conv = ValueConverter.GetConversion(typeof(IDictionary<string, object?>), typeof(TSourceType));
                if (conv != null)
                {
                    _parseDictionary = conv;
                }
                else if (GlobalSwitches.DynamicallyCompileToObject)
                {
                    // validate and compile the conversion from dictionary to object
                    _parseDictionary = ObjectExtensions.CompileToObject(typeof(TSourceType), this);
                }
                else
                {
                    // use reflection to convert the dictionary to object
                    _parseDictionary = ParseDictionaryViaReflection;
                }
            }
        }

        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// The default implementation uses <see cref="ObjectExtensions.ToObject(IDictionary{string, object?}, Type, IGraphType)"/> to convert the
        /// supplied field values into an object of type <typeparamref name="TSourceType"/>.
        /// When <see cref="GlobalSwitches.DynamicallyCompileToObject"/> is <see langword="true"/>, this method is compiled to a delegate
        /// during <see cref="Initialize"/> and the compiled delegate is used for all subsequent calls.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        public virtual object ParseDictionary(IDictionary<string, object?> value)
        {
            if (value == null)
                return null!;

            if (_parseDictionary != null)
                return _parseDictionary(value);

            // remainder of this method should not occur unless the user has overridden Initialize,
            // which is likely only when they wish to override ParseDictionary
            if (typeof(TSourceType) == typeof(object))
                return value;

            return ParseDictionaryViaReflection(value);
        }

        private object ParseDictionaryViaReflection(IDictionary<string, object?> value)
            => value.ToObject(typeof(TSourceType), this);

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
                    objectValue.Fields.Add(new GraphQLObjectField(new GraphQLName(field.Name), fieldValue));
                }
            }

            return objectValue;
        }

        private static object? GetFieldValue(FieldType field, object? value)
        {
            if (value == null)
                return null;

            // Given Field("FirstName", x => x.FName) and key == "FirstName" returns "FName"
            string propertyName = field.GetMetadata(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME, field.Name) ?? field.Name;
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
    }
}
