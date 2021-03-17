using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="double"/> value within a document.
    /// </summary>
    public class FloatValue : ValueNode<double>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public FloatValue(double value)
        {
            if (double.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be NaN.");

            Value = value;
        }
    }
}
