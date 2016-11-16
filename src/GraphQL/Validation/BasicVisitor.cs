using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class BasicVisitor
    {
        private readonly IEnumerable<INodeVisitor> _visitors;

        public BasicVisitor(params INodeVisitor[] visitors)
        {
            _visitors = visitors;
        }

        public void Visit(INode node)
        {
            if (node == null)
            {
                return;
            }

            _visitors.Apply(l => l.Enter(node));

            node.Children?.Apply(Visit);

            _visitors.ApplyReverse(l => l.Leave(node));
        }
    }
}
