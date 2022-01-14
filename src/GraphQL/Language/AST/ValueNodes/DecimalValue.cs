using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="decimal"/> value within a document.
    /// </summary>
    public class DecimalValue : GraphQLFloatValue, IValue<decimal>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public DecimalValue(decimal value)
        {
            ClrValue = value;
        }

        public decimal ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
