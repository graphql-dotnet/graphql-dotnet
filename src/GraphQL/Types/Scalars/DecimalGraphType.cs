using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Decimal scalar graph type represents a decimal value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="decimal"/> .NET values to this scalar graph type.
    /// </summary>
    public class DecimalGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            IntValue intVal => (decimal)intVal.ClrValue,
            LongValue longVal => (decimal)longVal.ClrValue,
            FloatValue floatVal => checked((decimal)floatVal.ClrValue),
            DecimalValue decVal => decVal.ClrValue,
            BigIntValue bigIntVal => checked((decimal)bigIntVal.ClrValue),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value)
        {
            try
            {
                return value switch
                {
                    GraphQLFloatValue v when v.ClrValue is double d => Ret(checked((decimal)d)),
                    GraphQLIntValue v when v.ClrValue is BigInteger b => Ret(checked((decimal)b)),
                    GraphQLIntValue _ => true,
                    GraphQLFloatValue _ => true,
                    GraphQLNullValue _ => true,
                    _ => false
                };
            }
            catch
            {
                return false;
            }

            static bool Ret(decimal _) => true;
        }

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            decimal _ => value,
            int i => checked((decimal)i),
            double d => checked((decimal)d),
            null => null,
            float f => checked((decimal)f),
            sbyte sb => checked((decimal)sb),
            byte b => checked((decimal)b),
            short s => checked((decimal)s),
            ushort us => checked((decimal)us),
            uint ui => checked((decimal)ui),
            long l => checked((decimal)l),
            ulong ul => checked((decimal)ul),
            BigInteger bi => (decimal)bi,
            _ => ThrowValueConversionError(value)
        };
    }
}
