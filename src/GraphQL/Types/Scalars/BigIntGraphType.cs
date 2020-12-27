using System.Numerics;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The BigInt scalar graph type represents a signed integer with any number of digits.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="BigInteger"/> .NET values to this scalar graph type.
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
