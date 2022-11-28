using System.Globalization;
using GraphQLParser;
using GraphQLParser.AST;

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
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLNullValue _ => null,
            GraphQLStringValue stringValue => ParseDate(stringValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            DateTime d => ValidateDate(d, value), // no boxing
            string stringValue => ParseDate(stringValue),
            null => null,
            _ => throw new FormatException($"Could not parse date. Expected either a string or a DateTime without time component. Value: {value}")
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            DateTime d => ValidateDate(d).ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo),
            null => null,
            _ => ThrowSerializationError(value)
        };

        private static DateTime ParseDate(ROM stringValue)
        {
            if (DateTime.TryParseExact(
#if NETSTANDARD2_0
                (string)
#endif
                stringValue, "yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date;
            }

            throw new FormatException($"Could not parse date. Expected yyyy-MM-dd. Value: {stringValue}");
        }

        private static object ValidateDate(DateTime value, object date)
        {
            ValidateDate(value);
            return date; // no boxing
        }

        private static DateTime ValidateDate(DateTime value)
        {
            if (value.TimeOfDay == TimeSpan.Zero)
                return value;

            throw new FormatException($"Expected date to have no time component. Value: {value}");
        }
    }
}
