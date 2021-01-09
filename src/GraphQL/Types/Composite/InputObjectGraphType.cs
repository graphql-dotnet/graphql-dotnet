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
        /// <inheritdoc/>
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

