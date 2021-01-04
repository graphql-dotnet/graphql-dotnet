namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="byte"/> value within a document.
    /// </summary>
    public class ByteValue : ValueNode<byte>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public ByteValue(byte value)
        {
            Value = value;
        }
    }
}
