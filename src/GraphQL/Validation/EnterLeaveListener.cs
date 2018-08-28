using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class EnterLeaveListener : INodeVisitor
    {
        private readonly List<INodeVisitor> _listeners =
            new List<INodeVisitor>();

        public EnterLeaveListener()
        {

        }

        public EnterLeaveListener(Action<EnterLeaveListener> configure)
        {
            configure(this);
        }

        void INodeVisitor.Enter(INode node)
        {
            foreach (var listener in _listeners)
            {
                listener.Enter(node);
            }
        }

        void INodeVisitor.Leave(INode node)
        {
            // Shouldn't this be done in reverse?
            foreach (var listener in _listeners)
            {
                listener.Leave(node);
            }
        }

        public void Match<TNode>(
            Action<TNode> enter = null,
            Action<TNode> leave = null)
            where TNode : INode
        {
            var listener = new MatchingNodeVisitor<TNode>(enter, leave);
            _listeners.Add(listener);
        }
    }
}
