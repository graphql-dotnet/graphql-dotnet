using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// An interface to handle events raised by a node walker such as <see cref="BasicVisitor"/>.
    /// </summary>
    public interface INodeVisitor
    {
        /// <summary>
        /// Raised when the node walker is entering a node.
        /// </summary>
        void Enter(INode node);

        /// <summary>
        /// Raised when the node walker is leaving a node.
        /// </summary>
        void Leave(INode node);
    }
}
