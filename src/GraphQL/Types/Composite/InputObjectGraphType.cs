using System.Collections.Generic;

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
        object ParseDictionary(IDictionary<string, object> value);
    }

    /// <inheritdoc/>
    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    /// <inheritdoc cref="IInputObjectGraphType"/>
    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
        /// <summary>
        /// Converts a supplied dictionary of keys and values to an object.
        /// The default implementation uses <see cref="ObjectExtensions.ToObject"/> to convert the
        /// supplied field values into an object of type <typeparamref name="TSourceType"/>.
        /// Overriding this method allows for customizing the deserialization process of input objects,
        /// much like a field resolver does for output objects. For example, you can set some 'computed'
        /// properties for your input object which were not passed in the GraphQL request.
        /// </summary>
        public virtual object ParseDictionary(IDictionary<string, object> value)
        {
            if (value == null)
                return null;

            // for InputObjectGraphType just return the dictionary
            if (typeof(TSourceType) == typeof(object))
                return value;

            // for InputObjectGraphType<TSourceType>, convert to TSourceType via ToObject.
            return value.ToObject(typeof(TSourceType), this);
        }
    }
}
