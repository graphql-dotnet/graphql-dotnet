using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// An interface to handle events raised by a node walker such as <see cref="BasicVisitor"/>.
    /// </summary>
    public interface INodeVisitor
    {
        /// <summary>
        /// Called when the node walker is entering a node.
        /// </summary>
        void Enter(ASTNode node, ValidationContext context);

        /// <summary>
        /// Called when the node walker is leaving a node.
        /// </summary>
        void Leave(ASTNode node, ValidationContext context);
    }
}
