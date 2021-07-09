#nullable enable

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the 'null' value within a document.
    /// </summary>
    public class NullValue : AbstractNode, IValue
    {
        object? IValue.Value => null;

        /// <inheritdoc/>
        public override string ToString() => "null";
    }
}
