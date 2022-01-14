using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="BigInteger"/> value within a document.
    /// </summary>
    public class BigIntValue : GraphQLIntValue, IValue<BigInteger>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public BigIntValue(BigInteger value)
        {
            ClrValue = value;
        }

        public BigInteger ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
