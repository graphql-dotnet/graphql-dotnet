using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;

namespace GraphQL.Validation
{
    public class MatchingNodeListener
    {
        public Func<INode, bool> Matches { get; set; }
        public Action<INode> Enter { get; set; }
        public Action<INode> Leave { get; set; }
    }

    public class EnterLeaveFuncListener : INodeVisitor
    {
        private readonly List<MatchingNodeListener> _listeners =
            new List<MatchingNodeListener>();

        public EnterLeaveFuncListener(Action<EnterLeaveFuncListener> configure)
        {
            configure(this);
        }

        public void Enter(INode node)
        {
            _listeners
                .Where(l => l.Enter != null && l.Matches(node))
                .Apply(l => l.Enter(node));
        }

        public void Leave(INode node)
        {
            _listeners
                .Where(l => l.Leave != null && l.Matches(node))
                .Apply(l => l.Leave(node));
        }

        public void Add<T>(
            Func<INode, bool> matches,
            Action<T> enter = null,
            Action<T> leave = null)
            where T : INode
        {
            if (enter == null && leave == null)
            {
                throw new ExecutionError("Must provide an enter or leave function.");
            }

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

    public class NodeVisitorMatchFuncListener<T> : INodeVisitor
        where T : INode
    {
        private readonly Func<INode, bool> _match;
        private readonly Action<T> _action;

        public NodeVisitorMatchFuncListener(Func<INode, bool> match, Action<T> action)
        {
            _match = match;
            _action = action;
        }

        public void Enter(INode node)
        {
            if (_match(node))
            {
                _action((T) node);
            }
        }

        public void Leave(INode node)
        {
        }
    }
}
