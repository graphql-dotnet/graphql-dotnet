namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="ushort"/> value within a document.
    /// </summary>
    public class UShortValue : ValueNode<ushort>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public UShortValue(ushort value)
        {
            Value = value;
        }
    }
}
