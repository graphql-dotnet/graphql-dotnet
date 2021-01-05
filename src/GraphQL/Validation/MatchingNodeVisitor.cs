using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class MatchingNodeVisitor<TNode> : INodeVisitor
        where TNode : INode
    {
        private readonly Action<TNode, ValidationContext> _enter;
        private readonly Action<TNode, ValidationContext> _leave;

        public MatchingNodeVisitor(Action<TNode, ValidationContext> enter = null, Action<TNode, ValidationContext> leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ArgumentException("Must provide an enter or leave function.");
            }

            _enter = enter;
            _leave = leave;
        }

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
