using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public sealed class NodeVisitors : INodeVisitor
    {
        private readonly INodeVisitor[] _nodeVisitors;
        public NodeVisitors(params INodeVisitor[] nodeVisitors)
        {
            _nodeVisitors = nodeVisitors ?? throw new ArgumentNullException(nameof(nodeVisitors));
        }

        void INodeVisitor.Enter(INode node, ValidationContext context)
        {
            foreach (var n in _nodeVisitors)
                n.Enter(node, context);
        }

        void INodeVisitor.Leave(INode node, ValidationContext context)
        {
            foreach (var n in _nodeVisitors)
                n.Leave(node, context);
        }
    }
}
