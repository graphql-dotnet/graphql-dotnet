namespace GraphQL.Language.AST
{
    public class DecimalValue : ValueNode<decimal>
    {
        public DecimalValue(decimal value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<decimal> other)
        {
            return Value == other.Value;
        }
    }
}