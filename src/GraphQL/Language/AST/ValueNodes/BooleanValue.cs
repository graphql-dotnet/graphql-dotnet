namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="bool"/> value within a document.
    /// </summary>
    public class BooleanValue : ValueNode<bool>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public BooleanValue(bool value) : base(value)
        {
        }
    }
}
