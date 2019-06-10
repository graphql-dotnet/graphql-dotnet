using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class DecimalGraphType : ScalarGraphType
    {
        public DecimalGraphType() => Name = "Decimal";

        public override object Serialize(object value) => ParseValue(value);

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(decimal));

        public override object ParseLiteral(IValue value)
        {
            if (value is StringValue stringValue)
            {
                return ParseValue(stringValue.Value);
            }

            if (value is IntValue intValue)
            {
                return ParseValue(intValue.Value);
            }

            if (value is LongValue longValue)
            {
                return ParseValue(longValue.Value);
            }

            if (value is FloatValue floatValue)
            {
                return ParseValue(floatValue.Value);
            }

            if (value is DecimalValue decimalValue)
            {
                return decimalValue.Value;
            }

            return null;
        }
    }
}
