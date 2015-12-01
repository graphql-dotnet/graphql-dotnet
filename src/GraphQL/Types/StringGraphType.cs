namespace GraphQL.Types
{
    public class StringGraphType : ScalarGraphType
    {
        public StringGraphType()
        {
            Name = "String";
        }

        public override object Coerce(object value)
        {
            return value != null ? ProcessString(value.ToString()) : null;
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
