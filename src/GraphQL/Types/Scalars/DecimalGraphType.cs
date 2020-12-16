using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class DecimalGraphType : ScalarGraphType
    {
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

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(decimal));
    }
}
