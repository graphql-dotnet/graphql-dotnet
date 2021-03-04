using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UInt scalar graph type represents an unsigned 32-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="uint"/> .NET values to this scalar graph type.
    /// </summary>
    public class UIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((uint)intValue.Value),
            LongValue longValue => checked((uint)longValue.Value),
            BigIntValue bigIntValue => checked((uint)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => uint.MinValue <= intValue.Value,
            LongValue longValue => uint.MinValue <= longValue.Value && longValue.Value <= uint.MaxValue,
            BigIntValue bigIntValue => uint.MinValue <= bigIntValue.Value && bigIntValue.Value <= uint.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            uint _ => value,
            null => null,
            int i => checked((uint)i),
            long l => checked((uint)l),
            sbyte sb => checked((uint)sb),
            byte b => checked((uint)b),
            short s => checked((uint)s),
            ushort us => checked((uint)us),
            ulong ul => checked((uint)ul),
            BigInteger bi => (uint)bi,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => value switch
        {
            uint _ => value,
            null => null,
            int i => checked((uint)i),
            long l => checked((uint)l),
            sbyte sb => checked((uint)sb),
            byte b => checked((uint)b),
            short s => checked((uint)s),
            ushort us => checked((uint)us),
            ulong ul => checked((uint)ul),
            BigInteger bi => checked((uint)bi),
            _ => ThrowSerializationError(value)
        };
    }
}
