using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="bool"/> value within a document.
    /// </summary>
    public class BooleanValue : GraphQLBooleanValue, IValue<bool>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public BooleanValue(bool value)
        {
            ClrValue = value;
        }

        public bool ClrValue { get; }

        object? IValue.ClrValue => ClrValue ? BoolBox.True : BoolBox.False;
    }
}
