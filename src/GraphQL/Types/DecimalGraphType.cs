namespace GraphQL.Types
{
    public class DecimalGraphType : ScalarGraphType
    {
        public DecimalGraphType()
        {
            Name = "Decimal";
        }

        public override object Coerce(object value)
        {
            decimal result;
            if (decimal.TryParse(value?.ToString() ?? string.Empty, out result))
            {
                return result;
            }
            return null;
        }
    }
}
