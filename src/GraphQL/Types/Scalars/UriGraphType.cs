using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Uri scalar graph type represents a string Uri specified in RFC 2396, RFC 2732, RFC 3986, and RFC 3987.
    /// </summary>
    public class UriGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Uri));

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            UriValue uriValue => uriValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };
    }
}
