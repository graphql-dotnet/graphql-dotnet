namespace GraphQL.Language.AST
{
    public class BooleanValue : ValueNode<bool>
    {
        public BooleanValue(bool value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<bool> other)
        {
            return Value == other.Value;
        }
    }
}