using System.Globalization;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The DateTimeOffset scalar graph type represents a date, time and offset from UTC.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="DateTimeOffset"/> .NET values to this scalar graph type.
    /// </summary>
    public class DateTimeOffsetGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetGraphType"/> class.
        /// </summary>
        public DateTimeOffsetGraphType()
        {
            Description =
                "The `DateTimeOffset` scalar type represents a date, time and offset from UTC. `DateTimeOffset` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLStringValue stringValue => ParseDate(stringValue.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            DateTimeOffset _ => value,
            DateTime d => d.Kind == DateTimeKind.Unspecified ? new DateTimeOffset(d, TimeSpan.Zero) : new DateTimeOffset(d), // handles Unspecified as UTC to preserve the same behavior as for System.Text.Json
            string s => ParseDate(s),
            null => null,
            _ => ThrowValueConversionError(value)
        };

        private static DateTimeOffset ParseDate(ROM stringValue)
        {
            // ISO-8601 format
            // Note that the "O" format is similar but always prints the fractional parts
            // of the second, which is not required by ISO-8601.
            if (DateTimeOffset.TryParseExact(
#if NETSTANDARD2_0
                (string)
#endif
                stringValue,
                "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }

            throw new FormatException($"Could not parse date. Expected ISO-8601 format. Value: {stringValue}");
        }

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            // ISO-8601 format
            // Note that the "O" format is similar but always prints the fractional parts
            // of the second, which is not required by ISO-8601.
            DateTimeOffset d => d.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", DateTimeFormatInfo.InvariantInfo), // ISO-8601 format (without unnecessary decimal places, allowed by ISO-8601)
            null => null,
            _ => ThrowSerializationError(value)
        };
    }
}
