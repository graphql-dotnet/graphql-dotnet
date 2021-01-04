namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="sbyte"/> value within a document.
    /// </summary>
    public class SByteValue : ValueNode<sbyte>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public SByteValue(sbyte value)
        {
            Value = value;
        }
    }
}
