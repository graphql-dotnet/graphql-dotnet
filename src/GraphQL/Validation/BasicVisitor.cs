using System.Collections;
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

            var children = node.Children;
            if (children != null)
            {
                if (children is IList list)
                {
                    for (int i = 0; i < list.Count; ++i)
                        Visit((INode)list[i]);
                }
                else foreach (var child in children)
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
