namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="ulong"/> value within a document.
    /// </summary>
    public class ULongValue : ValueNode<ulong>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public ULongValue(ulong value)
        {
            Value = value;
        }
    }
}
