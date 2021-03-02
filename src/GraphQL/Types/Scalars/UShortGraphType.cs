using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UShort scalar graph type represents an unsigned 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ushort"/> .NET values to this scalar graph type.
    /// </summary>
    public class UShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((ushort)intValue.Value),
            LongValue longValue => checked((ushort)longValue.Value),
            BigIntValue bigIntValue => checked((ushort)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => ushort.MinValue <= intValue.Value && intValue.Value <= ushort.MaxValue,
            LongValue longValue => ushort.MinValue <= longValue.Value && longValue.Value <= ushort.MaxValue,
            BigIntValue bigIntValue => ushort.MinValue <= bigIntValue.Value && bigIntValue.Value <= ushort.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            ushort _ => value,
            null => null,
            int i => checked((ushort)i),
            sbyte sb => checked((ushort)sb),
            byte b => checked((ushort)b),
            short s => checked((ushort)s),
            uint ui => checked((ushort)ui),
            long l => checked((ushort)l),
            ulong ul => checked((ushort)ul),
            BigInteger bi => (ushort)bi,
            float f => checked((ushort)f),
            double d => checked((ushort)d),
            decimal d => checked((ushort)d),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            ushort _ => value,
            null => null,
            int i => checked((ushort)i),
            sbyte sb => checked((ushort)sb),
            byte b => checked((ushort)b),
            short s => checked((ushort)s),
            uint ui => checked((ushort)ui),
            long l => checked((ushort)l),
            ulong ul => checked((ushort)ul),
            BigInteger bi => (ushort)bi,
            _ => ThrowSerializationError(value)
        };
    }
}
