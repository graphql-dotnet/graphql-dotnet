namespace GraphQL.Language.AST
{
    public class ByteValue : ValueNode<byte>
    {
        public ByteValue(byte value) => Value = value;

        protected override bool Equals(ValueNode<byte> other) => Value == other.Value;
    }
}
