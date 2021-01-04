namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a named type node within a document.
    /// </summary>
    public class NamedType : AbstractNode, IType
    {
        /// <summary>
        /// Initializes a new named type node containing the specified name.
        /// </summary>
        public NamedType(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the name of the named type node.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc/>
        public override string ToString() => $"NamedType{{name={Name}}}";
    }
}
