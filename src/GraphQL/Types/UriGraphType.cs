using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class UriGraphType : ScalarGraphType
    {
        public UriGraphType() => Name = "Uri";

        public override object Serialize(object value) => ParseValue(value);

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Uri));

        public override object ParseLiteral(IValue value)
        {
            if (value is UriValue uriValue)
            {
                return uriValue.Value;
            }

            if (value is StringValue stringValue)
            {
                return ParseValue(stringValue.Value);
            }

            return null;
        }
    }
}
