using System.Numerics;
using GraphQL.Language.AST;

#nullable enable

namespace GraphQL.Types
{
    /// <summary>
    /// The BigInt scalar graph type represents a signed integer with any number of digits.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="BigInteger"/> .NET values to this scalar graph type.
    /// </summary>
    public class BigIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => new BigInteger(intValue.Value),
            LongValue longValue => new BigInteger(longValue.Value),
            BigIntValue bigIntValue => bigIntValue.Value,
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue _ => true,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            BigInteger _ => value,
            null => null,
            int i => new BigInteger(i),
            sbyte sb => new BigInteger(sb),
            byte b => new BigInteger(b),
            short s => new BigInteger(s),
            ushort us => new BigInteger(us),
            uint ui => new BigInteger(ui),
            long l => new BigInteger(l),
            ulong ul => new BigInteger(ul),
            _ => ThrowValueConversionError(value)
        };
    }
}
