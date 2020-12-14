using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Decimal scalar graph type represents a decimal value.
    /// </summary>
    public class DecimalGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(decimal));

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            DecimalValue decimalValue => decimalValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            IntValue intValue => ParseValue(intValue.Value),
            LongValue longValue => ParseValue(longValue.Value),
            FloatValue floatValue => ParseValue(floatValue.Value),
            BigIntValue bigIntValue => ParseValue(bigIntValue.Value),
            _ => null
        };
    }
}
