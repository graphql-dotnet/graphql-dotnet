namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="long"/> value within a document.
    /// </summary>
    public class LongValue : ValueNode<long>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public LongValue(long value) : base(value)
        {
        }
    }
}
