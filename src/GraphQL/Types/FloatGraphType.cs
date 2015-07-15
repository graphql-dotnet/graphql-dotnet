namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public FloatGraphType()
        {
            Name = "Float";
        }

        public override object Coerce(object value)
        {
            float result;
            if (float.TryParse(value.ToString(), out result))
            {
                return result;
            }
            return null;
        }
    }
}
