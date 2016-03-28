using GraphQL.Language;

namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public FloatGraphType()
        {
            Name = "Float";
        }

        public override object ParseValue(object value)
        {
            double result;
            if (double.TryParse(value?.ToString() ?? string.Empty, out result))
            {
                return result;
            }
            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            var floatVal = value as FloatValue;
            return floatVal?.Value;
        }
    }
}
