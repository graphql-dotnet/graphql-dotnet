using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The UInt scalar graph type represents an unsigned 32-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="uint"/> .NET values to this scalar graph type.
    /// </summary>
    public class UIntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => checked((uint)intValue.ClrValue),
            LongValue longValue => checked((uint)longValue.ClrValue),
            BigIntValue bigIntValue => checked((uint)bigIntValue.ClrValue),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => uint.MinValue <= intValue.ClrValue,
            LongValue longValue => uint.MinValue <= longValue.ClrValue && longValue.ClrValue <= uint.MaxValue,
            BigIntValue bigIntValue => uint.MinValue <= bigIntValue.ClrValue && bigIntValue.ClrValue <= uint.MaxValue,
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
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
            _ => ThrowValueConversionError(value)
        };
    }
}
