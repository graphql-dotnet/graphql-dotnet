namespace GraphQL.Language.AST
{
    public class NullValue : AbstractNode, IValue
    {
        object IValue.Value => null;

        /// <inheritdoc />
        public override string ToString() => "null";

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return true;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType();
        }
    }
}
