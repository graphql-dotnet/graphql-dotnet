using System;
using System.Globalization;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class DateGraphType : ScalarGraphType
    {
        public DateGraphType()
        {
            Name = "Date";
            Description =
                "The `Date` scalar type represents a timestamp provided in UTC. `Date` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;
                return dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
            }

            string inputValue = value?.ToString().Trim('"');

            DateTime outputValue;
            if (DateTime.TryParse(
                inputValue,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AdjustToUniversal,
                out outputValue))
            {
                return outputValue;
            }

            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is DateTimeValue)
            {
                return ParseValue(((DateTimeValue)value).Value);
            }

            if (value is StringValue)
            {
                return ParseValue(((StringValue)value).Value);
            }

            return null;
        }
    }
}
