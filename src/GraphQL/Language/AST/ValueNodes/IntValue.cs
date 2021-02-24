namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="int"/> value within a document.
    /// </summary>
    public class IntValue : ValueNode<int>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public IntValue(int value) : base(value)
        {
        }
    }
}
