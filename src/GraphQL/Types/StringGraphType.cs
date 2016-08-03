using GraphQL.Language;
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
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            return value != null ? ProcessString(value.ToString()) : null;
        }

        public override object ParseLiteral(IValue value)
        {
            var stringValue = value as StringValue;

            return stringValue != null && stringValue.Value != null ? ProcessString(stringValue.Value) : null;
        }

        private string ProcessString(string value)
        {
            value = value.Replace("\\\"", "\"");
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}
