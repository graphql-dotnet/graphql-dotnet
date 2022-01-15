using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UShort scalar graph type represents an unsigned 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ushort"/> .NET values to this scalar graph type.
    /// </summary>
    public class UShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => checked((ushort)intValue.ClrValue),
            LongValue longValue => checked((ushort)longValue.ClrValue),
            BigIntValue bigIntValue => checked((ushort)bigIntValue.ClrValue),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => ushort.MinValue <= intValue.ClrValue && intValue.ClrValue <= ushort.MaxValue,
            LongValue longValue => ushort.MinValue <= longValue.ClrValue && longValue.ClrValue <= ushort.MaxValue,
            BigIntValue bigIntValue => ushort.MinValue <= bigIntValue.ClrValue && bigIntValue.ClrValue <= ushort.MaxValue,
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
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
            BigInteger bi => checked((ushort)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}
