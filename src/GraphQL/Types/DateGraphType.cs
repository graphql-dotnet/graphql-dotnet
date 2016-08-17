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
            if (value is DateTime)
            {
                return value;
            }

            string inputValue = (string)value;
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
                return ((DateTimeValue)value).Value.ToString("o");
            }

            return null;
        }
    }
}
