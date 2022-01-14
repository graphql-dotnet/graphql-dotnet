using System.Numerics;
using GraphQL.Language.AST;
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
            FloatValue floatVal => floatVal.ClrValue,
            IntValue intVal => (double)intVal.ClrValue,
            LongValue longVal => (double)longVal.ClrValue,
            DecimalValue decVal => checked((double)decVal.ClrValue),
            BigIntValue bigIntVal => checked((double)bigIntVal.ClrValue),
            NullValue _ => null,
            GraphQLValue v and not IValue => ParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value)
        {
            try
            {
                return value switch
                {
                    LongValue _ => true,
                    DecimalValue decVal => Ret(checked((double)decVal.ClrValue)),
                    BigIntValue bigIntVal => Ret(checked((double)bigIntVal.ClrValue)),
                    FloatValue _ => true,
                    IntValue _ => true,
                    NullValue _ => true,
                    GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
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
