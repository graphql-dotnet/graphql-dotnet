using System;
using System.Globalization;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class DecimalGraphType : ScalarGraphType
    {
        public DecimalGraphType()
        {
            Name = "Decimal";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            decimal result;

            if (decimal.TryParse(value?.ToString() ?? string.Empty, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is StringValue)
            {
                return ParseValue(((StringValue)value).Value);
            }
  
            if (value is LongValue)
            {
                return Convert.ToDecimal(((LongValue)value).Value);
            }

            if (value is FloatValue)
            {
                return Convert.ToDecimal(((FloatValue)value).Value);
            }

            if (value is DecimalValue)
            {
                return ((DecimalValue) value).Value;
            }

            return null;
        }
    }
}
