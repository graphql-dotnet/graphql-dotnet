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
        private readonly Func<TNode, ValidationContext, ValueTask>? _enter;
        private readonly Func<TNode, ValidationContext, ValueTask>? _leave;

        /// <summary>
        /// Returns a new instance configured with the specified enter/leave delegates.
        /// </summary>
        public MatchingNodeVisitor(Action<TNode, ValidationContext>? enter = null, Action<TNode, ValidationContext>? leave = null)
            : this(FromAction(enter), FromAction(leave))
        {
        }

        private static Func<TNode, ValidationContext, ValueTask>? FromAction(Action<TNode, ValidationContext>? action)
        {
            if (action == null)
            {
                return null;
            }
            return (node, context) =>
            {
                action(node, context);
                return default;
            };
        }

        /// <inheritdoc cref="MatchingNodeVisitor{TNode}(Action{TNode, ValidationContext}?, Action{TNode, ValidationContext}?)"/>
        public MatchingNodeVisitor(Func<TNode, ValidationContext, ValueTask>? enter = null, Func<TNode, ValidationContext, ValueTask>? leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
        }

        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            if (_enter != null && node is TNode n)
            {
                return _enter(n, context);
            }
            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (_leave != null && node is TNode n)
            {
                return _leave(n, context);
            }
            return default;
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

        ValueTask INodeVisitor.EnterAsync(ASTNode node, ValidationContext context)
        {
            if (_enter != null && node is TNode n)
            {
                _enter(n, context, _state);
            }
            return default;
        }

        ValueTask INodeVisitor.LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (_leave != null && node is TNode n)
            {
                _leave(n, context, _state);
            }
            return default;
        }
    }
}
