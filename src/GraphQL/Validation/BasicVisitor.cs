using System.Collections.Generic;
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

            for (int i = 0; i < _visitors.Count; i++)
            {
                _visitors[i].Enter(node);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    Visit(child);
                }
            }

            for (int i = _visitors.Count - 1; i >= 0; i--)
            {
                _visitors[i].Leave(node);
            }
        }
    }
}
