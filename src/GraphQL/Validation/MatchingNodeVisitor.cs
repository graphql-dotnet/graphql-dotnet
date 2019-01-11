using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class MatchingNodeVisitor<TNode> : INodeVisitor
        where TNode : INode
    {
        private readonly Action<TNode> _enter;
        private readonly Action<TNode> _leave;

        public MatchingNodeVisitor(Action<TNode> enter = null, Action<TNode> leave = null)
        {
            if (enter == null && leave == null)
            {
                throw new ExecutionError("Must provide an enter or leave function.");
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
