using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class StringGraphType : ScalarGraphType
    {
        public StringGraphType()
        {
            Name = "String";
        }

        public override object Serialize(object value)
        {
            return value;
        }

        public override object ParseValue(object value)
        {
            return value?.ToString();
        }

        public override object ParseLiteral(IValue value)
        {
            var stringValue = value as StringValue;
            return stringValue?.Value;
        }
    }
}
