#nullable enable

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node that can have directives.
    /// </summary>
    public interface IHaveDirectives : INode
    {
        /// <summary>
        /// Gets or sets a list of directive nodes for this node.
        /// </summary>
        public Directives? Directives { get; set; }
    }
}
