namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="decimal"/> value within a document.
    /// </summary>
    public class DecimalValue : ValueNode<decimal>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public DecimalValue(decimal value)
        {
            Value = value;
        }
    }
}
