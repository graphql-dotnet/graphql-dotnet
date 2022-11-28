using System.Globalization;
using GraphQLParser.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Guid scalar graph type represents a 128-bit globally unique identifier (GUID).
    /// </summary>
    public class GuidGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLStringValue s => Guid.Parse(
#if NETSTANDARD2_0
                (string)
#endif
                s.Value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLStringValue s => Guid.TryParse(
#if NETSTANDARD2_0
                (string)
#endif
                s.Value, out _),
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            Guid _ => value, // no boxing
            string s => Guid.Parse(s),
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => value switch
        {
            Guid _ => true,
            string s => Guid.TryParse(s, out _),
            null => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value) => value switch
        {
            Guid g => g.ToString("D", CultureInfo.InvariantCulture),
            null => null,
            _ => ThrowSerializationError(value)
        };
    }
}
