namespace GraphQL.Language.AST
{
    public class FloatValue : ValueNode<double>
    {
        public FloatValue(double value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<double> other)
        {
            return Value == other.Value;
        }
    }
}