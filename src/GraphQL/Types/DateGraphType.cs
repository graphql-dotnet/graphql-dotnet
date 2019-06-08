using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class DateGraphType : ScalarGraphType
    {
        public DateGraphType()
        {
            Name = "Date";
            Description = "The `Date` scalar type represents a year, month and day in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object Serialize(object value)
        {
            var date = ParseValue(value);

            if (date is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            return null;
        }

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
