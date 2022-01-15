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
        public override object? ParseLiteral(GraphQLValue value)
        {
            object? Parse(object? v) =>
                v switch
                {
                    double x => x,
                    int x => (double)x,
                    long x => (double)x,
                    decimal x => checked((double)x),
                    BigInteger x => checked((double)x),
                    _ => ThrowLiteralConversionError(value)
                };

            return value switch
            {
                GraphQLIntValue x => Parse(x.ClrValue),
                GraphQLFloatValue x => Parse(x.ClrValue),
                GraphQLNullValue _ => null,
                _ => ThrowLiteralConversionError(value)
            };
        }

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value)
        {
            bool CanParse(object? v) =>
                v switch
                {
                    long _ => true,
                    decimal x => Ret(checked((double)x)),
                    BigInteger x => Ret(checked((double)x)),
                    double _ => true,
                    int _ => true,
                    _ => false
                };

            try
            {
                return value switch
                {
                    GraphQLIntValue x => CanParse(x),
                    GraphQLFloatValue x => CanParse(x),
                    GraphQLNullValue _ => true,
                    _ => false
                };
            }
            catch
            {
                return false;
            }

            static bool Ret(double _) => true;
        }

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
