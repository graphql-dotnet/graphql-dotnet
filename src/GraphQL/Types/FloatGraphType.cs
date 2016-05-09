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
            double result;
            if (double.TryParse(value?.ToString() ?? string.Empty, out result))
            {
                return result;
            }
            return null;
        }
    }
}
