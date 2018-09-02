namespace GraphQL.Language.AST
{
    public class ULongValue : ValueNode<ulong>
    {
        public ULongValue(ulong value) => Value = value;

        protected override bool Equals(ValueNode<ulong> other) => Value == other.Value;
    }
}
