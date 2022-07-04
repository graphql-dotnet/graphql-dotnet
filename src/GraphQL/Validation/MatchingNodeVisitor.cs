using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// A node listener which runs configured delegates only when the node entered/left matches the specified node type.
    /// </summary>
    /// <typeparam name="TNode">A specified AST node type.</typeparam>
    public class MatchingNodeVisitor<TNode> : INodeVisitor
        where TNode : ASTNode
    {
        private readonly Action<TNode, ValidationContext>? _enter;
        private readonly Action<TNode, ValidationContext>? _leave;

        /// <summary>
        /// Returns a new instance configured with the specified enter/leave delegates.
        /// </summary>
        public MatchingNodeVisitor(Action<TNode, ValidationContext>? enter = null, Action<TNode, ValidationContext>? leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
        }

        void INodeVisitor.Enter(ASTNode node, ValidationContext context)
        {
            if (_enter != null && node is TNode n)
            {
                _enter(n, context);
            }
        }

        void INodeVisitor.Leave(ASTNode node, ValidationContext context)
        {
            if (_leave != null && node is TNode n)
            {
                _leave(n, context);
            }
        }
    }

    /// <summary>
    /// A node listener which runs configured delegates only when the node entered/left matches the specified node type.
    /// </summary>
    /// <typeparam name="TNode">A specified AST node type.</typeparam>
    /// <typeparam name="TState">Type of the provided state.</typeparam>
    public class MatchingNodeVisitor<TNode, TState> : INodeVisitor
        where TNode : ASTNode
    {
        private readonly Action<TNode, ValidationContext, TState?>? _enter;
        private readonly Action<TNode, ValidationContext, TState?>? _leave;
        private readonly TState? _state;

        /// <summary>
        /// Returns a new instance configured with the specified enter/leave delegates and arbitrary state.
        /// </summary>
        public MatchingNodeVisitor(TState? state, Action<TNode, ValidationContext, TState?>? enter = null, Action<TNode, ValidationContext, TState?>? leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
            _state = state;
        }

        void INodeVisitor.Enter(ASTNode node, ValidationContext context)
        {
            if (_enter != null && node is TNode n)
            {
                _enter(n, context, _state);
            }
        }

        void INodeVisitor.Leave(ASTNode node, ValidationContext context)
        {
            if (_leave != null && node is TNode n)
            {
                _leave(n, context, _state);
            }
        }
    }
}
