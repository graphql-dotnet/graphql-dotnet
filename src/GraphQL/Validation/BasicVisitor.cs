using System.Collections;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Walks an AST node tree executing <see cref="INodeVisitor.Enter(INode, ValidationContext)"/>
    /// and <see cref="INodeVisitor.Leave(INode, ValidationContext)"/> methods for each node.
    /// </summary>
    public class BasicVisitor
    {
        private readonly IList<INodeVisitor> _visitors;

        /// <summary>
        /// Returns a new instance configured for the specified list of <see cref="INodeVisitor"/>.
        /// </summary>
        public BasicVisitor(params INodeVisitor[] visitors)
        {
            _visitors = visitors;
        }

        /// <inheritdoc cref="BasicVisitor.BasicVisitor(INodeVisitor[])"/>
        public BasicVisitor(IList<INodeVisitor> visitors)
        {
            _visitors = visitors;
        }

        /// <summary>
        /// Walks the specified <see cref="INode"/>, executing <see cref="INodeVisitor.Enter(INode, ValidationContext)"/> and
        /// <see cref="INodeVisitor.Leave(INode, ValidationContext)"/> methods for each node.
        /// </summary>
        public void Visit(INode node, ValidationContext context)
        {
            if (node == null)
            {
                return;
            }

            for (int i = 0; i < _visitors.Count; i++)
            {
                _visitors[i].Enter(node, context);
            }

            var children = node.Children;
            if (children != null)
            {
                if (children is IList list)
                {
                    for (int i = 0; i < list.Count; ++i)
                        Visit((INode)list[i], context);
                }
                else
                    foreach (var child in children)
                    {
                        Visit(child, context);
                    }
            }

            for (int i = _visitors.Count - 1; i >= 0; i--)
            {
                _visitors[i].Leave(node, context);
            }
        }
    }
}
