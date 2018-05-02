using GraphQL.Language.AST;

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
            if (value is bool)
            {
                return (bool) value;
            }

            return false;
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
