namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="uint"/> value within a document.
    /// </summary>
    public class UIntValue : ValueNode<uint>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public UIntValue(uint value)
        {
            Value = value;
        }
    }
}
