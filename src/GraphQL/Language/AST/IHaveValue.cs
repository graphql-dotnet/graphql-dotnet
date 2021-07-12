#nullable enable

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a node that has child value node.
    /// </summary>
    public interface IHaveValue : INode
    {
        /// <summary>
        /// Returns the value node containing the value.
        /// </summary>
        IValue Value { get; }
    }
}
