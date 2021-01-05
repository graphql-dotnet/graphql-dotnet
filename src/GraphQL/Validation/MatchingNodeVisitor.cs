using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// A node listener which runs configured delegates only when the node entered/left matches the specified node type.
    /// </summary>
    /// <typeparam name="TNode">A specified AST node type.</typeparam>
    public class MatchingNodeVisitor<TNode> : INodeVisitor
        where TNode : INode
    {
        private readonly Action<TNode, ValidationContext> _enter;
        private readonly Action<TNode, ValidationContext> _leave;
        private readonly Func<ValidationContext, bool> _shouldRun;

        /// <summary>
        /// Returns a new instance configured with the specified enter/leave delegates.
        /// </summary>
        public MatchingNodeVisitor(Action<TNode, ValidationContext> enter = null, Action<TNode, ValidationContext> leave = null, Func<ValidationContext, bool> shouldRun = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
            _shouldRun = shouldRun;
        }

        /// <inheritdoc/>
        public bool ShouldRunOn(ValidationContext context) => _shouldRun?.Invoke(context) ?? true;

        void INodeVisitor.Enter(INode node, ValidationContext context)
        {
            if (_enter != null && node is TNode n)
            {
                _enter(n, context);
            }
        }

        void INodeVisitor.Leave(INode node, ValidationContext context)
        {
            if (_leave != null && node is TNode n)
            {
                _leave(n, context);
            }
        }
    }
}
