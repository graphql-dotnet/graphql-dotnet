namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an enumeration value (specified by a string) within a document.
    /// </summary>
    public class EnumValue : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance with a specified string representation of the enumeration value.
        /// </summary>
        public EnumValue(string name)
        {
            Name = name;
        }

        object IValue.Value => Name;

        /// <summary>
        /// Returns the string representation of the enumeration value.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc/>
        public override string ToString() => $"EnumValue{{name={Name}}}";
    }
}
