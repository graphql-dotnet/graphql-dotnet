using System.Numerics;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Decimal scalar graph type represents a decimal value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="decimal"/> .NET values to this scalar graph type.
    /// </summary>
    public class DecimalGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            IntValue intVal => (decimal)intVal.Value,
            LongValue longVal => (decimal)longVal.Value,
            FloatValue floatVal => checked((decimal)floatVal.Value),
            DecimalValue decVal => decVal.Value,
            BigIntValue bigIntVal => checked((decimal)bigIntVal.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value)
        {
            try
            {
                return value switch
                {
                    IntValue _ => true,
                    LongValue _ => true,
                    FloatValue f => Ret(checked((decimal)f.Value)),
                    DecimalValue _ => true,
                    BigIntValue b => Ret(checked((decimal)b.Value)),
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
        public override object ParseValue(object value) => value switch
        {
            decimal _ => value,
            int i => checked((decimal)i),
            null => null,
            float f => checked((decimal)f),
            double d => checked((decimal)value),
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
