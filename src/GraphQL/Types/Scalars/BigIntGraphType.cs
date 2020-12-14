using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The BigInt scalar graph type represents a signed integer with any number of digits.
    /// </summary>
    public class BigIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            BigIntValue bigIntValue => bigIntValue.Value,
            LongValue longValue => new BigInteger(longValue.Value),
            IntValue intValue => new BigInteger(intValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(BigInteger));
    }
}
