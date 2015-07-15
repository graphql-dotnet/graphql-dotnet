namespace GraphQL.Types
{
    public class IntGraphType : ScalarGraphType
    {
        public IntGraphType()
        {
            Name = "Int";
        }

        public override object Coerce(object value)
        {
            int result;
            if (int.TryParse(value.ToString(), out result))
            {
                return result;
            }
            return null;
        }
    }
}
