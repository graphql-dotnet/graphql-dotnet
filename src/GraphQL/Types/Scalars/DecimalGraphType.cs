using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class DecimalGraphType : ScalarGraphType
    {
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(decimal));

        public override object ParseLiteral(IValue value) => value switch
        {
            StringValue stringValue => ParseValue(stringValue.Value),
            IntValue intValue => ParseValue(intValue.Value),
            LongValue longValue => ParseValue(longValue.Value),
            FloatValue floatValue => ParseValue(floatValue.Value),
            DecimalValue decimalValue => decimalValue.Value,
            _ => null
        };
    }
}
