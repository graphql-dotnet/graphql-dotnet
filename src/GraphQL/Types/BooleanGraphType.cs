using GraphQL.Language.AST;
using System;

namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType
    {
        public BooleanGraphType()
        {
            Name = "Boolean";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            return ValueConverter.ConvertTo(value, typeof(bool));
        }

        public override object ParseLiteral(IValue value)
        {
            var boolVal = value as BooleanValue;
            return boolVal?.Value;
        }
    }
}
