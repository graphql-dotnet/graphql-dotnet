using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Validation
{
    /// <summary>
    /// Walks an AST node tree executing <see cref="INodeVisitor.Enter(ASTNode, ValidationContext)"/>
    /// and <see cref="INodeVisitor.Leave(ASTNode, ValidationContext)"/> methods for each node.
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
        /// Walks the specified <see cref="ASTNode"/>, executing <see cref="INodeVisitor.Enter(ASTNode, ValidationContext)"/> and
        /// <see cref="INodeVisitor.Leave(ASTNode, ValidationContext)"/> methods for each node.
        /// </summary>
        public override async ValueTask VisitAsync(ASTNode? node, State context)
        {
            if (node != null)
            {
                for (int i = 0; i < _visitors.Count; ++i)
                    _visitors[i].Enter(node, context.Context);

                await base.VisitAsync(node, context);

                for (int i = _visitors.Count - 1; i >= 0; --i)
                    _visitors[i].Leave(node, context.Context);
            }
        }

        public readonly struct State : IASTVisitorContext
        {
            public State(ValidationContext context)
            {
                Context = context;
            }

            public ValidationContext Context { get; }

            public CancellationToken CancellationToken => Context.CancellationToken;
        }
    }
}
