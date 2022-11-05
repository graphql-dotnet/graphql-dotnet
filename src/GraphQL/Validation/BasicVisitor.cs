using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation
{
    /// <summary>
    /// Walks an AST node tree executing <see cref="INodeVisitor.EnterAsync(ASTNode, ValidationContext)"/>
    /// and <see cref="INodeVisitor.LeaveAsync(ASTNode, ValidationContext)"/> methods for each node.
    /// </summary>
    public class BasicVisitor : ASTVisitor<BasicVisitor.State>
    {
        private readonly IList<INodeVisitor> _visitors;

        /// <summary>
        /// Returns a new instance configured for the specified list of <see cref="INodeVisitor"/>.
        /// </summary>
        public BasicVisitor(params INodeVisitor[] visitors)
        {
            _visitors = visitors;
        }

        /// <inheritdoc cref="BasicVisitor(INodeVisitor[])"/>
        public BasicVisitor(IList<INodeVisitor> visitors)
        {
            _visitors = visitors;
        }

        /// <summary>
        /// Walks the specified <see cref="ASTNode"/>, executing <see cref="INodeVisitor.EnterAsync(ASTNode, ValidationContext)"/> and
        /// <see cref="INodeVisitor.LeaveAsync(ASTNode, ValidationContext)"/> methods for each node.
        /// </summary>
        public override async ValueTask VisitAsync(ASTNode? node, State context)
        {
            if (node != null)
            {
                for (int i = 0; i < _visitors.Count; ++i)
                    await _visitors[i].EnterAsync(node, context.Context).ConfigureAwait(false);

                await base.VisitAsync(node, context).ConfigureAwait(false);

                for (int i = _visitors.Count - 1; i >= 0; --i)
                    await _visitors[i].LeaveAsync(node, context.Context).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IASTVisitorContext"/>
        public readonly struct State : IASTVisitorContext
        {
            /// <summary>
            /// Initializes a new instance with the specified validation context.
            /// </summary>
            public State(ValidationContext context)
            {
                Context = context;
            }

            /// <summary>
            /// Returns the validation context.
            /// </summary>
            public ValidationContext Context { get; }

            /// <inheritdoc/>
            public CancellationToken CancellationToken => Context.CancellationToken;
        }
    }
}
