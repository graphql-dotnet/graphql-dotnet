using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class EnterLeaveListener : INodeVisitor
    {
        private readonly List<INodeVisitor> _visitors =
            new List<INodeVisitor>();

        public EnterLeaveListener()
        {

        }

        public EnterLeaveListener(Action<EnterLeaveListener> configure)
        {
            configure(this);
        }

        void INodeVisitor.Enter(INode node, ValidationContext context)
        {
            foreach (var visitor in _visitors)
            {
                visitor.Enter(node, context);
            }
        }

        void INodeVisitor.Leave(INode node, ValidationContext context)
        {
            // Shouldn't this be done in reverse?
            foreach (var visitor in _visitors)
            {
                visitor.Leave(node, context);
            }
        }

        public void Match<TNode>(
            Action<TNode, ValidationContext> enter = null,
            Action<TNode, ValidationContext> leave = null)
            where TNode : INode
        {
            var listener = new MatchingNodeVisitor<TNode>(enter, leave);
            _visitors.Add(listener);
        }
    }
}
