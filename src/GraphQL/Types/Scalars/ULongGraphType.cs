using System.Numerics;
using GraphQL.Language.AST;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ULong scalar graph type represents an unsigned 64-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="ulong"/> .NET values to this scalar graph type.
    /// </summary>
    public class ULongGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => checked((ulong)intValue.ClrValue),
            LongValue longValue => checked((ulong)longValue.ClrValue),
            BigIntValue bigIntValue => checked((ulong)bigIntValue.ClrValue),
            NullValue _ => null,
            GraphQLValue v and not IValue => ParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue bigIntValue => ulong.MinValue <= bigIntValue.ClrValue && bigIntValue.ClrValue <= ulong.MaxValue,
            NullValue _ => true,
            GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
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
