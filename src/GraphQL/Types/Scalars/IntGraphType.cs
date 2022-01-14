using System.Numerics;
using GraphQL.Language.AST;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="int"/> .NET values to this scalar graph type.
    /// </summary>
    public class IntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intValue => intValue.ClrValue,
            LongValue longValue => checked((int)longValue.ClrValue),
            BigIntValue bigIntValue => checked((int)bigIntValue.ClrValue),
            NullValue _ => null,
            GraphQLValue v and not IValue => ParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            IntValue _ => true,
            LongValue longValue => int.MinValue <= longValue.ClrValue && longValue.ClrValue <= int.MaxValue,
            BigIntValue bigIntValue => int.MinValue <= bigIntValue.ClrValue && bigIntValue.ClrValue <= int.MaxValue,
            NullValue _ => true,
            GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            int _ => value,
            null => null,
            sbyte sb => checked((int)sb),
            byte b => checked((int)b),
            short s => checked((int)s),
            ushort us => checked((int)us),
            uint ui => checked((int)ui),
            long l => checked((int)l),
            ulong ul => checked((int)ul),
            BigInteger bi => checked((int)bi),
            _ => ThrowValueConversionError(value)
        };
    }
}
