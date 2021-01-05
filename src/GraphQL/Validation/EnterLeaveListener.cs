using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// A <see cref="INodeVisitor"/> which allows for easy configuration of multiple child node listeners
    /// each of which only respond to the type of node that they are configured for.
    /// </summary>
    public class EnterLeaveListener : INodeVisitor
    {
        private readonly List<INodeVisitor> _visitors = new List<INodeVisitor>();
        private readonly Func<ValidationContext, bool> _shouldRun;

        /// <summary>
        /// Initializes a new instance and runs the supplied configuration delegate.
        /// </summary>
        public EnterLeaveListener(Action<EnterLeaveListener> configure, Func<ValidationContext, bool> shouldRun = null)
        {
            configure(this);
            _shouldRun = shouldRun;
        }

        /// <inheritdoc/>
        public bool ShouldRunOn(ValidationContext context) => _shouldRun?.Invoke(context) ?? true;

        void INodeVisitor.Enter(INode node, ValidationContext context)
        {
            foreach (var visitor in _visitors)
            {
                if (visitor.ShouldRunOn(context))
                    visitor.Enter(node, context);
            }
        }

        void INodeVisitor.Leave(INode node, ValidationContext context)
        {
            // Shouldn't this be done in reverse?
            foreach (var visitor in _visitors)
            {
                if (visitor.ShouldRunOn(context))
                    visitor.Leave(node, context);
            }
        }

        /// <summary>
        /// Configures an event listener for the specified node type.
        /// </summary>
        /// <typeparam name="TNode">The type of the AST node to listen for.</typeparam>
        /// <param name="enter">A delegate to execute when the node of specified type is entered.</param>
        /// <param name="leave">A delegate to execute when the node of specified type is left.</param>
        public void Match<TNode>(
            Action<TNode, ValidationContext> enter = null,
            Action<TNode, ValidationContext> leave = null)
            where TNode : INode
        {
            _visitors.Add(new MatchingNodeVisitor<TNode>(enter, leave));
        }
    }
}
