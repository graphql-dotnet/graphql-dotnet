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
        private readonly List<INodeVisitor> _listeners =
            new List<INodeVisitor>();

        /// <summary>
        /// Initializes a new instance with no configured listeners.
        /// </summary>
        public EnterLeaveListener()
        {

        }

        /// <summary>
        /// Initializes a new instance and runs the supplied configuration delegate.
        /// </summary>
        public EnterLeaveListener(Action<EnterLeaveListener> configure)
        {
            configure(this);
        }

        void INodeVisitor.Enter(INode node)
        {
            foreach (var listener in _listeners)
            {
                listener.Enter(node);
            }
        }

        void INodeVisitor.Leave(INode node)
        {
            // Shouldn't this be done in reverse?
            foreach (var listener in _listeners)
            {
                listener.Leave(node);
            }
        }

        /// <summary>
        /// Configures an event listener for the specified node type.
        /// </summary>
        /// <typeparam name="TNode">The type of the AST node to listen for.</typeparam>
        /// <param name="enter">A delegate to execute when the node of specified type is entered.</param>
        /// <param name="leave">A delegate to execute when the node of specified type is left.</param>
        public void Match<TNode>(
            Action<TNode> enter = null,
            Action<TNode> leave = null)
            where TNode : INode
        {
            var listener = new MatchingNodeVisitor<TNode>(enter, leave);
            _listeners.Add(listener);
        }
    }
}
