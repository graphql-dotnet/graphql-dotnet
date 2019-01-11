namespace GraphQL.Language.AST
{
    public class ShortValue : ValueNode<short>
    {
        public ShortValue(short value) => Value = value;

        protected override bool Equals(ValueNode<short> other) => Value == other.Value;

    }
}
