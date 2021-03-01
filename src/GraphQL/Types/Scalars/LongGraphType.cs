using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Long scalar graph type represents a signed 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="long"/> .NET values to this scalar graph type.
    /// </summary>
    public class LongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((long)intValue.Value),
            LongValue longValue => checked((long)longValue.Value),
            BigIntValue bigIntValue => checked((long)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue bigIntValue => long.MinValue <= bigIntValue.Value && bigIntValue.Value <= long.MaxValue,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            long _ => value,
            null => null,
            int i => checked((long)i),
            sbyte sb => checked((long)sb),
            byte b => checked((long)b),
            short s => checked((long)s),
            ushort us => checked((long)us),
            uint ui => checked((long)ui),
            ulong ul => checked((long)ul),
            BigInteger bi => (long)bi,
            _ => ThrowValueConversionError(value)
        };
    }
}
