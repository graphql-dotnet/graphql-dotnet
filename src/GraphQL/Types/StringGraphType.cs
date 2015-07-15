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
            return value != null ? value.ToString().Trim('"') : null;
        }
    }
}
