using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class BasicVisitor
    {
        private readonly IList<INodeVisitor> _visitors;

        public BasicVisitor(params INodeVisitor[] visitors)
        {
            _visitors = visitors;
        }

        public BasicVisitor(IList<INodeVisitor> visitors)
        {
            _visitors = visitors;
        }

        public void Visit(INode node)
        {
            if (node == null)
            {
                return;
            }

            foreach (var visitor in _visitors)
            {
                visitor.Enter(node);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    Visit(child);
                }
            }

            foreach (var visitor in _visitors.Reverse())
            {
                visitor.Leave(node);
            }
        }
    }
}
