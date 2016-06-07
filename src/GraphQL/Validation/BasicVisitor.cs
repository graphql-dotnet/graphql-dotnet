using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;

namespace GraphQL.Validation
{
    public class BasicVisitor
    {
        private readonly IEnumerable<INodeVisitor> _visitors;

        public BasicVisitor(IEnumerable<INodeVisitor> visitors)
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

            if (node.Children != null && node.Children.Any())
            {
                node.Children.Apply(Visit);
            }

            _visitors.Apply(l => l.Leave(node));
        }
    }
}
