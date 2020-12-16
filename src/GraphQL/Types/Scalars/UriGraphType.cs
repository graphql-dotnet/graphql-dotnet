using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class UriGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            UriValue uriValue => uriValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Uri));
    }
}
