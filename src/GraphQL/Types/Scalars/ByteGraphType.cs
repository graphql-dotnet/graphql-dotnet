using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Byte scalar graph type represents an unsigned 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="byte"/> .NET values to this scalar graph type.
    /// </summary>
    public class ByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((byte)intValue.Value),
            LongValue longValue => checked((byte)longValue.Value),
            BigIntValue bigIntValue => checked((byte)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => byte.MinValue <= intValue.Value && intValue.Value <= byte.MaxValue,
            LongValue longValue => byte.MinValue <= longValue.Value && longValue.Value <= byte.MaxValue,
            BigIntValue bigIntValue => byte.MinValue <= bigIntValue.Value && bigIntValue.Value <= byte.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            byte _ => value,
            null => null,
            int i => checked((byte)i),
            sbyte sb => checked((byte)sb),
            short s => checked((byte)s),
            ushort us => checked((byte)us),
            uint ui => checked((byte)ui),
            long l => checked((byte)l),
            ulong ul => checked((byte)ul),
            BigInteger bi => (byte)bi,
            float f => checked((byte)f),
            double d => checked((byte)d),
            decimal d => checked((byte)d),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            byte _ => value,
            null => null,
            int i => checked((byte)i),
            sbyte sb => checked((byte)sb),
            short s => checked((byte)s),
            ushort us => checked((byte)us),
            uint ui => checked((byte)ui),
            long l => checked((byte)l),
            ulong ul => checked((byte)ul),
            BigInteger bi => checked((byte)bi),
            _ => ThrowSerializationError(value)
        };
    }
}
