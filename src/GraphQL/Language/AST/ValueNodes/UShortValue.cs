namespace GraphQL.Language.AST
{
    public class UShortValue : ValueNode<ushort>
    {
        public UShortValue(ushort value) => Value = value;

        protected override bool Equals(ValueNode<ushort> other) => Value == other.Value;
    }
}
