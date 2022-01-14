using System.Numerics;
using GraphQL.Language.AST;
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
            DecimalValue decVal => decVal.Value,
            BigIntValue bigIntVal => checked((decimal)bigIntVal.ClrValue),
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
                    IntValue _ => true,
                    LongValue _ => true,
                    FloatValue f => Ret(checked((decimal)f.ClrValue)),
                    DecimalValue _ => true,
                    BigIntValue b => Ret(checked((decimal)b.ClrValue)),
                    NullValue _ => true,
                    GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
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
