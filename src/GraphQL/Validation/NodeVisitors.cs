using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents a set of <see cref="INodeVisitor"/> instances that each runs upon entering or leaving a node.
    /// Be aware that all <see cref="INodeVisitor"/> instances are called in the order supplied; not in reverse order upon leaving a node.
    /// </summary>
    public sealed class NodeVisitors : INodeVisitor
    {
        private readonly INodeVisitor[] _nodeVisitors;

        /// <summary>
        /// Initializes a new instance with the specified <see cref="INodeVisitor"/>s.
        /// </summary>
        public NodeVisitors(params INodeVisitor[] nodeVisitors)
        {
            _nodeVisitors = nodeVisitors ?? throw new ArgumentNullException(nameof(nodeVisitors));
        }

        async ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            foreach (var n in _nodeVisitors)
                await n.EnterAsync(node, context).ConfigureAwait(false);
        }

        async ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
        {
            foreach (var n in _nodeVisitors)
                await n.LeaveAsync(node, context).ConfigureAwait(false);
        }
    }
}
