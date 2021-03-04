using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Short scalar graph type represents a signed 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="short"/> .NET values to this scalar graph type.
    /// </summary>
    public class ShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((short)intValue.Value),
            LongValue longValue => checked((short)longValue.Value),
            BigIntValue bigIntValue => checked((short)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue,
            LongValue longValue => short.MinValue <= longValue.Value && longValue.Value <= short.MaxValue,
            BigIntValue bigIntValue => short.MinValue <= bigIntValue.Value && bigIntValue.Value <= short.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            short _ => value,
            null => null,
            int i => checked((short)i),
            sbyte sb => checked((short)sb),
            byte b => checked((short)b),
            ushort us => checked((short)us),
            uint ui => checked((short)ui),
            long l => checked((short)l),
            ulong ul => checked((short)ul),
            BigInteger bi => (short)bi,
            float f => checked((short)f),
            double d => checked((short)d),
            decimal d => checked((short)d),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            short _ => value,
            null => null,
            int i => checked((short)i),
            sbyte sb => checked((short)sb),
            byte b => checked((short)b),
            ushort us => checked((short)us),
            uint ui => checked((short)ui),
            long l => checked((short)l),
            ulong ul => checked((short)ul),
            BigInteger bi => checked((short)bi),
            _ => ThrowSerializationError(value)
        };
    }
}
