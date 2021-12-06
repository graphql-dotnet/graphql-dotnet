#if NET6_0_OR_GREATER

using System;
using System.Globalization;
using GraphQL.Language.AST;

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
        public override object? ParseLiteral(IValue value) => value switch
        {
            NullValue _ => null,
            StringValue stringValue => ParseDate(stringValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            DateOnly _ => value, // no boxing
            string stringValue => ParseDate(stringValue),
            null => null,
            _ => throw new FormatException($"Could not parse date. Expected either a string or a DateOnly. Value: {value}")
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            DateOnly d => d.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo),
            null => null,
            _ => ThrowSerializationError(value)
        };

        private static DateOnly ParseDate(string stringValue)
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
