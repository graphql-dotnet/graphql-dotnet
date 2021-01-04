namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the 'null' value within a document.
    /// </summary>
    public class NullValue : AbstractNode, IValue
    {
        object IValue.Value => null;

        /// <inheritdoc/>
        public override string ToString() => "null";

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return true;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType();
        }
    }
}
