using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class GuidGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            var guidValue = value as GuidValue;
            return guidValue?.Value;
        }

        public override object ParseValue(object value) =>
            ValueConverter.ConvertTo(value, typeof(Guid));

        public override object Serialize(object value) => ParseValue(value);
    }
}
