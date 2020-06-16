using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class UriGraphType : ScalarGraphType
    {
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Uri));

        public override object ParseLiteral(IValue value) => value switch
        {
            UriValue uriValue => uriValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };
    }
}
