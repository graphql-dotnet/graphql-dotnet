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
            DecimalValue decimalValue => decimalValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            IntValue intValue => checked((decimal)intValue.Value),
            LongValue longValue => checked((decimal)longValue.Value),
            FloatValue floatValue => checked((decimal)floatValue.Value),
            BigIntValue bigIntValue => checked((decimal)bigIntValue.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(decimal));
    }
}
