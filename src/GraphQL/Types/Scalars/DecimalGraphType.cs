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
            GraphQLIntValue x => Decimal.Parse(x.Value),
            GraphQLFloatValue x => Decimal.Parse(x.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLIntValue x => Decimal.TryParse(x.Value, out var _),
            GraphQLFloatValue x => Decimal.TryParse(x.Value, out var _),
            GraphQLNullValue _ => true,
            _ => false
        };

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
