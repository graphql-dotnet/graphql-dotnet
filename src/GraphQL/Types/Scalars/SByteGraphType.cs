using System.Numerics;
using GraphQL.Language.AST;

#nullable enable

namespace GraphQL.Types
{
    /// <summary>
    /// The SByte scalar graph type represents a signed 8-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="sbyte"/> .NET values to this scalar graph type.
    /// </summary>
    public class SByteGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => checked((sbyte)intValue.Value),
            LongValue longValue => checked((sbyte)longValue.Value),
            BigIntValue bigIntValue => checked((sbyte)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue intValue => sbyte.MinValue <= intValue.Value && intValue.Value <= sbyte.MaxValue,
            LongValue longValue => sbyte.MinValue <= longValue.Value && longValue.Value <= sbyte.MaxValue,
            BigIntValue bigIntValue => sbyte.MinValue <= bigIntValue.Value && bigIntValue.Value <= sbyte.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            sbyte _ => value,
            null => null,
            int i => checked((sbyte)i),
            byte b => checked((sbyte)b),
            short s => checked((sbyte)s),
            ushort us => checked((sbyte)us),
            uint ui => checked((sbyte)ui),
            long l => checked((sbyte)l),
            ulong ul => checked((sbyte)ul),
            BigInteger bi => checked((sbyte)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}
