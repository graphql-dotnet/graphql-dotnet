namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="short"/> value within a document.
    /// </summary>
    public class ShortValue : ValueNode<short>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public ShortValue(short value)
        {
            Value = value;
        }

    }
}
