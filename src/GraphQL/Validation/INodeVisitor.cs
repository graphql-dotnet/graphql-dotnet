using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// An interface to handle events raised by a node walker such as <see cref="BasicVisitor"/>.
    /// </summary>
    public interface INodeVisitor
    {
        /// <summary>
        /// Indicates the applicability of this visitor to the given <see cref="ValidationContext"/>.
        /// <br/>
        /// Some visitors process only documents with certain nodes and it makes no sense to run them on other documents.
        /// </summary>
        bool ShouldRunOn(ValidationContext context);

        /// <summary>
        /// Called when the node walker is entering a node.
        /// </summary>
        void Enter(INode node, ValidationContext context);

        /// <summary>
        /// Called when the node walker is leaving a node.
        /// </summary>
        void Leave(INode node, ValidationContext context);
    }
}
