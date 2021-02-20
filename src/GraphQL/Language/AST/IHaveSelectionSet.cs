namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node that has child field selection nodes.
    /// </summary>
    public interface IHaveSelectionSet : INode
    {
        /// <summary>
        /// Gets or sets a list of child field selection nodes for this node.
        /// </summary>
        SelectionSet SelectionSet { get; set; }
    }
}
