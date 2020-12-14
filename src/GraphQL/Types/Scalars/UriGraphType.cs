using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Uri scalar graph type represents an Uri represented as a string value.
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
