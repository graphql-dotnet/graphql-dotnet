#if NET6_0_OR_GREATER

using System.Globalization;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Time scalar graph type represents a time in accordance with the ISO-8601 standard.
    /// Format is `HH:mm:ss.FFFFFFF`.
    /// </summary>
    public class TimeOnlyGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOnlyGraphType"/> class.
        /// </summary>
        public TimeOnlyGraphType()
        {
            Description = "The `Time` scalar type represents a time in accordance with the " +
                "[ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard. Format is `HH:mm:ss.FFFFFFF`.";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLNullValue _ => null,
            GraphQLStringValue stringValue => ParseTime(stringValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            TimeOnly _ => value, // no boxing
            string stringValue => ParseTime(stringValue),
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            TimeOnly d => d.ToString("HH:mm:ss.FFFFFFF", DateTimeFormatInfo.InvariantInfo),
            null => null,
            _ => ThrowSerializationError(value)
        };

        private static TimeOnly ParseTime(ROM stringValue)
        {
            if (TimeOnly.TryParseExact(stringValue, "HH:mm:ss.FFFFFFF", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var time))
            {
                return time;
            }

            throw new FormatException($"Could not parse time. Expected HH:mm:ss.FFFFFFF. Value: {stringValue}");
        }
    }
}

#endif
