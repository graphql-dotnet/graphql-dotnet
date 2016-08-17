using System;
using System.Globalization;
using GraphQL.Language;

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
            string inputValue;
            DateTime dateTime;

            if (value is DateTime)
            {
                inputValue = ((DateTime)value).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'");
            }
            else
            {
                inputValue = value != null ? value.ToString().Trim('"') : string.Empty;
            }

            if (DateTime.TryParse(inputValue, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal,
                out dateTime))
            {
                return dateTime;
            }
            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is StringValue)
            {
                return ParseValue(((StringValue)value).Value);
            }

            return null;
        }
    }
}
