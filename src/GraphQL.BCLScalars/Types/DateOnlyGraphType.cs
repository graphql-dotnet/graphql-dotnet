#if NET6_0_OR_GREATER

using System.Globalization;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Date scalar graph type represents a year, month and day in accordance with the ISO-8601 standard.
    /// Format is `yyyy-MM-dd`.
    /// </summary>
    public class DateOnlyGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateOnlyGraphType"/> class.
        /// </summary>
        public DateOnlyGraphType()
        {
            Description = "The `Date` scalar type represents a year, month and day in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard. Format is `yyyy-MM-dd`";
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
            DateOnly _ => value, // no boxing
            string stringValue => ParseDate(stringValue),
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            DateOnly d => d.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo),
            null => null,
            _ => ThrowSerializationError(value)
        };

        private static DateOnly ParseDate(ROM stringValue)
        {
            if (DateOnly.TryParseExact(stringValue, "yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var date))
            {
                return date;
            }

            throw new FormatException($"Could not parse date. Expected yyyy-MM-dd. Value: {stringValue}");
        }
    }
}

#endif
