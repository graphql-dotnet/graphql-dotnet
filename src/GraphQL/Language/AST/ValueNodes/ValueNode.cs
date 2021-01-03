namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node which contains an literal value within a document.
    /// </summary>
    public abstract class ValueNode<T> : AbstractNode, IValue<T>
    {
        /// <inheritdoc/>
        public T Value { get; protected set; }

        object IValue.Value => Value;

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}{{value={Value}}}";

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((T)obj);
        }

        /// <summary>
        /// Compares the value of this instance to another instance.
        /// </summary>
        protected abstract bool Equals(ValueNode<T> node);
    }
}
