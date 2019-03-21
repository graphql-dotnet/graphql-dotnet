using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class DateTimeGraphType : ScalarGraphType
    {
        public DateTimeGraphType()
        {
            Name = "DateTime";
            Description =
                "The `DateTime` scalar type represents a date and time. `DateTime` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object Serialize(object value) => ParseValue(value);

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(DateTime));

        public override object ParseLiteral(IValue value)
        {
            if (value is DateTimeValue timeValue)
            {
                return timeValue.Value;
            }

            if (value is StringValue stringValue)
            {
                return ParseValue(stringValue.Value);
            }

            return null;
        }
    }
}
