namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node that has name.
    /// </summary>
    public interface IHaveName : INode
    {
        /// <summary>
        /// Returns the name of this node.
        /// </summary>
        NameNode NameNode { get; }
    }
}
