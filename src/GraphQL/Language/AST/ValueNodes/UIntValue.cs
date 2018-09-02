namespace GraphQL.Language.AST
{
    public class UIntValue : ValueNode<uint>
    {
        public UIntValue(uint value) => Value = value;

        protected override bool Equals(ValueNode<uint> other) => Value == other.Value;
    }
}
