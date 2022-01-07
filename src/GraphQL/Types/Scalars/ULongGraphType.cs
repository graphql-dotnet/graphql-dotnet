using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ULong scalar graph type represents an unsigned 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ulong"/> .NET values to this scalar graph type.
    /// </summary>
    public class ULongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((ulong)intValue.Value),
            LongValue longValue => checked((ulong)longValue.Value),
            BigIntValue bigIntValue => checked((ulong)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue bigIntValue => ulong.MinValue <= bigIntValue.Value && bigIntValue.Value <= ulong.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            ulong _ => value,
            null => null,
            int i => checked((ulong)i),
            long l => checked((ulong)l),
            BigInteger bi => checked((ulong)bi),
            sbyte sb => checked((ulong)sb),
            byte b => checked((ulong)b),
            short s => checked((ulong)s),
            ushort us => checked((ulong)us),
            uint ui => checked((ulong)ui),
            _ => ThrowValueConversionError(value)
        };
    }
}
