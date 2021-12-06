#if NET6_0_OR_GREATER

using System;
using System.Globalization;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Time scalar graph type represents a time in accordance with the ISO-8601 standard.
    /// Format is `HH:mm:ss.fffffff`.
    /// </summary>
    public class TimeOnlyGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlyGraphType"/> class.
        /// </summary>
        public TimeOnlyGraphType()
        {
            Description = "The `Time` scalar type represents a time in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard. Format is `HH:mm:ss.fffffff`.";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(IValue value) => value switch
        {
            NullValue _ => null,
            StringValue stringValue => ParseTime(stringValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            TimeOnly _ => value, // no boxing
            string stringValue => ParseTime(stringValue),
            null => null,
            _ => throw new FormatException($"Could not parse time. Expected either a string or a TimeOnly. Value: {value}")
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            TimeOnly d => d.ToString("HH:mm:ss.FFFFFFF", DateTimeFormatInfo.InvariantInfo),
            null => null,
            _ => ThrowSerializationError(value)
        };

        private static TimeOnly ParseTime(string stringValue)
        {
            if (TimeOnly.TryParseExact(stringValue, "HH:mm:ss.FFFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var date))
            {
                return date;
            }

            throw new FormatException($"Could not parse time. Expected HH:mm:ss.FFFFFFF. Value: {stringValue}");
        }
    }
}

#endif
