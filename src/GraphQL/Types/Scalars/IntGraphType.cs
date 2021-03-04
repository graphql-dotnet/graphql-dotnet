using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="int"/> .NET values to this scalar graph type.
    /// </summary>
    public class IntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intValue => intValue.Value,
            LongValue longValue => checked((int)longValue.Value),
            BigIntValue bigIntValue => checked((int)bigIntValue.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            IntValue _ => true,
            LongValue longValue => int.MinValue <= longValue.Value && longValue.Value <= int.MaxValue,
            BigIntValue bigIntValue => int.MinValue <= bigIntValue.Value && bigIntValue.Value <= int.MaxValue,
            NullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
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
            BigInteger bi => (int)bi,
            _ => ThrowValueConversionError(value)
        };
    }
}
