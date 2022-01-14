using System;
using System.Globalization;
using GraphQL.Language.AST;
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
            StringValue s => Guid.Parse(s.ClrValue),
            NullValue _ => null,
            GraphQLValue v and not IValue => ParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            //TODO: TryParse can work with Span on netstandard2.1
            StringValue s => Guid.TryParse(s.ClrValue, out _),
            NullValue _ => true,
            GraphQLValue v and not IValue => CanParseLiteral((GraphQLValue)Language.CoreToVanillaConverter.Value(v)),
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
