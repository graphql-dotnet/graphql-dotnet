using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class GuidGraphType : ScalarGraphType
    {
        public GuidGraphType() => Name = "Guid";

        public override object ParseLiteral(IValue value)
        {
            if (value is GuidValue guidValue)
            {
                return guidValue.Value;
            }

            if (value is StringValue stringValue)
            {
                return ParseValue(stringValue.Value);
            }

            return null;
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(Guid));

        public override object Serialize(object value) => ParseValue(value);
    }
}
