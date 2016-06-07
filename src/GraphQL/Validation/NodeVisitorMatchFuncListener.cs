using System;
using GraphQL.Language;

namespace GraphQL.Validation
{
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
