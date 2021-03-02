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
            IntValue intValue => BoxValue(intValue.Value),
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

        private static readonly object _boxedNeg1 = -1;
        private static readonly object _boxed0 = 0;
        private static readonly object _boxed1 = 1;
        private static readonly object _boxed2 = 2;
        private static readonly object _boxed3 = 3;
        private static readonly object _boxed4 = 4;
        private static readonly object _boxed5 = 5;
        private static object BoxValue(int value) => value switch
        {
            0 => _boxed0,
            1 => _boxed1,
            2 => _boxed2,
            3 => _boxed3,
            4 => _boxed4,
            5 => _boxed5,
            -1 => _boxedNeg1,
            _ => value
        };
    }
}
