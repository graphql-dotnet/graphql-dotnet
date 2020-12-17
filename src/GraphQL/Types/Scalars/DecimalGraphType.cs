using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// The Decimal scalar graph type represents a decimal value.
    /// By default <see cref="GraphTypeTypeRegistry"/> maps all <see cref="decimal"/> .NET values to this scalar graph type.
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
