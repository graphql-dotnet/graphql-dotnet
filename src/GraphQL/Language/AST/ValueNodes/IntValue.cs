using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="int"/> value within a document.
    /// </summary>
    public class IntValue : GraphQLIntValue, IValue<int>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public IntValue(int value)
        {
            ClrValue = value;
        }

        public int ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
