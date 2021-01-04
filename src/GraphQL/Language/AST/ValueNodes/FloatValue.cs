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
            Value = value;
        }

        /// <inheritdoc/>
        protected override bool Equals(ValueNode<double> other) => Value == other.Value;
    }
}
