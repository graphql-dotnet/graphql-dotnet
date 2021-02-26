using System;
using System.Globalization;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Date scalar graph type represents a year, month and day in accordance with the ISO-8601 standard.
    /// </summary>
    public class DateGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateGraphType"/> class.
        /// </summary>
        public DateGraphType()
        {
            Description = "The `Date` scalar type represents a year, month and day in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object Serialize(object value)
        {
            var date = ParseValue(value);

            if (date is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return null;
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value)
            => value is StringValue stringValue ? ParseValue(stringValue.Value) : null;

        /// <inheritdoc/>
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
                if (DateTime.TryParseExact(valueAsString, "yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
                {
                    return date;
                }
                throw new FormatException($"Could not parse date. Expected yyyy-MM-dd. Value: {valueAsString}");
            }

            throw new FormatException($"Could not parse date. Expected either a string or a DateTime without time component. Value: {value}");
        }

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new StringValue((string)Serialize((DateTime)value));
    }
}
