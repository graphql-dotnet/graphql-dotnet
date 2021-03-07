using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Uri scalar graph type represents a string Uri specified in RFC 2396, RFC 2732, RFC 3986, and RFC 3987.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="Uri"/> .NET values to this scalar graph type.
    /// </summary>
    public class UriGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            StringValue s => new Uri(s.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            string s => new Uri(s),
            Uri _ => value,
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override object Serialize(object value) => ParseValue(value)?.ToString();
    }
}
