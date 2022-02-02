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
            GraphQLIntValue x => ParseDoubleAccordingSpec(x),
            GraphQLFloatValue x => ParseDoubleAccordingSpec(x),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => TryParse(x.Value),
            GraphQLFloatValue x => TryParse(x.Value),
            GraphQLNullValue _ => true,
            _ => false
        };

        // IsNaN checks not really necessary because text from parser cannot represent NaN
        private bool TryParse(ReadOnlySpan<char> chars)
            => Double.TryParse(chars, out var number) /* && !double.IsNaN(number) */ && !double.IsInfinity(number);

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
