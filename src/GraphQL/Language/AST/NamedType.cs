namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a named type node within a document.
    /// </summary>
    public class NamedType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a new named type node containing the specified <see cref="NameNode"/>.
        /// </summary>
        public NamedType(NameNode node)
        {
            NameNode = node;
        }

        /// <summary>
        /// Returns the name of the named type node.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of the type.
        /// </summary>
        public NameNode NameNode { get; }

        /// <inheritdoc/>
        public override string ToString() => $"NamedType{{name={Name}}}";
    }
}
