namespace GraphQL.Language.AST
{
    public class SByteValue : ValueNode<sbyte>
    {
        public SByteValue(sbyte value) => Value = value;

        protected override bool Equals(ValueNode<sbyte> other) => Value == other.Value;
    }
}
