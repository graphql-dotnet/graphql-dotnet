using System;
using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Float scalar graph type represents an IEEE 754 double-precision floating point value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="double"/> and <see cref="float"/> .NET values to this scalar graph type.
    /// </summary>
    public class FloatGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => AssertValid(Double.Parse(x.Value)),
            GraphQLFloatValue x => AssertValid(Double.Parse(x.Value)),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Double.TryParse(x.Value, out double v) && IsValid(v),
            GraphQLFloatValue x => Double.TryParse(x.Value, out double v) && IsValid(v),
            GraphQLNullValue _ => true,
            _ => false
        };

        private static double AssertValid(double d)
            => IsValid(d)
                ? d
                : throw new OverflowException("Value was either too large or too small for a Double.");

        private static bool IsValid(double d)
            => !double.IsNegativeInfinity(d) && !double.IsPositiveInfinity(d) && !double.IsNaN(d);

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            double _ => value,
            int i => checked((double)i),
            null => null,
            float f => checked((double)f),
            decimal d => checked((double)d),
            sbyte sb => checked((double)sb),
            byte b => checked((double)b),
            short s => checked((double)s),
            ushort us => checked((double)us),
            uint ui => checked((double)ui),
            long l => checked((double)l),
            ulong ul => checked((double)ul),
            BigInteger bi => (double)bi,
            _ => ThrowValueConversionError(value)
        };
    }
}
