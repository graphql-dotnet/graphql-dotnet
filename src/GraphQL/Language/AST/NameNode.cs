namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a name within a document. This could be the name of a field, type, argument, directive, alias, etc.
    /// </summary>
    public class NameNode : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance with the specified name.
        /// </summary>
        public NameNode(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the contained name.
        /// </summary>
        public string Name { get; }
    }
}
