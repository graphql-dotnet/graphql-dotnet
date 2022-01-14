using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="long"/> value within a document.
    /// </summary>
    public class LongValue : GraphQLIntValue, IValue<long>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public LongValue(long value)
        {
            ClrValue = value;
        }

        public long ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
