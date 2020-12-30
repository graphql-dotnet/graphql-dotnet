namespace GraphQL.Language.AST
{
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        public T Value { get; protected set; }

        object IValue.Value => Value;

        /// <inheritdoc />
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((T)obj);
        }

        protected abstract bool Equals(ValueNode<T> node);
    }
}
