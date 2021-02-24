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
        public override object ParseLiteral(IValue value)
            => value is StringValue stringValue ? ParseValue(stringValue.Value) : null;

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Uri));

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new StringValue(ParseValue(value).ToString());
    }
}
