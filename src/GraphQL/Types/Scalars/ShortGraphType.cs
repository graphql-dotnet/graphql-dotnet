using System.Numerics;
using GraphQL.Language.AST;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Short scalar graph type represents a signed 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="short"/> .NET values to this scalar graph type.
    /// </summary>
    public class ShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => checked((short)intValue.ClrValue),
            LongValue longValue => checked((short)longValue.ClrValue),
            BigIntValue bigIntValue => checked((short)bigIntValue.ClrValue),
            NullValue _ => null,
            GraphQLValue v and not IValue => ParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => short.MinValue <= intValue.ClrValue && intValue.ClrValue <= short.MaxValue,
            LongValue longValue => short.MinValue <= longValue.ClrValue && longValue.ClrValue <= short.MaxValue,
            BigIntValue bigIntValue => short.MinValue <= bigIntValue.ClrValue && bigIntValue.ClrValue <= short.MaxValue,
            NullValue _ => true,
            GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
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
            _ => ThrowValueConversionError(value)
        };
    }
}
