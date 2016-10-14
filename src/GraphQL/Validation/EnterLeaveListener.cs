using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class MatchingNodeListener
    {
        public Func<INode, bool> Matches { get; set; }
        public Action<INode> Enter { get; set; }
        public Action<INode> Leave { get; set; }
    }

    public class EnterLeaveListener : INodeVisitor
    {
        private readonly List<MatchingNodeListener> _listeners =
            new List<MatchingNodeListener>();

        public EnterLeaveListener(Action<EnterLeaveListener> configure)
        {
            configure(this);
        }

        void INodeVisitor.Enter(INode node)
        {
            _listeners
                .Where(l => l.Enter != null && l.Matches(node))
                .Apply(l => l.Enter(node));
        }

        void INodeVisitor.Leave(INode node)
        {
            _listeners
                .Where(l => l.Leave != null && l.Matches(node))
                .Apply(l => l.Leave(node));
        }

        public void Match<T>(
            Action<T> enter = null,
            Action<T> leave = null)
            where T : INode
        {
            if (enter == null && leave == null)
            {
                throw new ExecutionError("Must provide an enter or leave function.");
            }

            Func<INode, bool> matches = n => n.GetType().IsAssignableFrom(typeof(T));

            var listener = new MatchingNodeListener
            {
                Matches = matches
            };

            if (enter != null)
            {
                listener.Enter = n => enter((T) n);
            }

            if (leave != null)
            {
                listener.Leave = n => leave((T) n);
            }

            _listeners.Add(listener);
        }
    }
}
