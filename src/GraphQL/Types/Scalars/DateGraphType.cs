using GraphQL.Language.AST;
using System;
using System.Globalization;

namespace GraphQL.Types
{
    public class DateGraphType : ScalarGraphType
    {
        public DateGraphType()
        {
            Description = "The `Date` scalar type represents a year, month and day in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        public override object Serialize(object value)
        {
            var date = ParseValue(value);

            if (date is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return null;
        }

        public override object ParseValue(object value)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime.TimeOfDay == TimeSpan.Zero)
                {
                    return dateTime;
                }
                throw new FormatException($"Expected date to have no time component. Value: {value}");
            }

            if (value is string valueAsString)
            {
                if (DateTime.TryParseExact(valueAsString, "yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out var date))
                {
                    return date.ToUniversalTime();
                }
                throw new FormatException($"Could not parse date. Expected yyyy-MM-dd. Value: {valueAsString}");
            }

            throw new FormatException($"Could not parse date. Expected either a string or a DateTime without time component. Value: {value}");
        }

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
