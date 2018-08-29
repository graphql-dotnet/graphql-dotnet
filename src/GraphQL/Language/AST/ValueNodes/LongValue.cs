namespace GraphQL.Language.AST
{
    public class LongValue : ValueNode<long>
    {
        public LongValue(long value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<long> other)
        {
            return Value == other.Value;
        }
    }
}