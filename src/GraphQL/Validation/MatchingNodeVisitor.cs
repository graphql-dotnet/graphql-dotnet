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
        private readonly Action<TNode> _enter;
        private readonly Action<TNode> _leave;

        /// <summary>
        /// Returns a new instance configured with the specified enter/leave delegates.
        /// </summary>
        public MatchingNodeVisitor(Action<TNode> enter = null, Action<TNode> leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
        }

        void INodeVisitor.Enter(INode node)
        {
            if (_enter != null && node is TNode n)
            {
                _enter(n);
            }
        }

        void INodeVisitor.Leave(INode node)
        {
            if (_leave != null && node is TNode n)
            {
                _leave(n);
            }
        }
    }
}
