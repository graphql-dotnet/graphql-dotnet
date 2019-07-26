namespace GraphQL.Language.AST
{
    public class IntValue : ValueNode<int>
    {
        public IntValue(int value) => Value = value;

        protected override bool Equals(ValueNode<int> other) => Value == other.Value;
    }
}
