using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class GuidGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value) => value switch
        {
            GuidValue guidValue => guidValue.Value,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => null
        };

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Guid));
    }
}
