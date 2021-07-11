#nullable enable

using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Walks an AST node tree executing <see cref="INodeVisitor.Enter(INode, ValidationContext)"/>
    /// and <see cref="INodeVisitor.Leave(INode, ValidationContext)"/> methods for each node.
    /// </summary>
    public readonly struct BasicVisitor
    {
        // https://github.com/dotnet/roslyn/issues/39869
        private static readonly Action<INode, State> _visitDelegate = VisitRecursive;

        private readonly IList<INodeVisitor> _visitors;

        /// <summary>
        /// Returns a new instance configured for the specified list of <see cref="INodeVisitor"/>.
        /// </summary>
        public BasicVisitor(params INodeVisitor[] visitors)
        {
            _visitors = visitors;
        }

        /// <inheritdoc cref="BasicVisitor(INodeVisitor[])"/>
        public BasicVisitor(IList<INodeVisitor> visitors)
        {
            _visitors = visitors;
        }

        /// <summary>
        /// Walks the specified <see cref="INode"/>, executing <see cref="INodeVisitor.Enter(INode, ValidationContext)"/> and
        /// <see cref="INodeVisitor.Leave(INode, ValidationContext)"/> methods for each node.
        /// </summary>
        public void Visit(INode node, ValidationContext context) => VisitRecursive(node, new State(context, _visitors));

        private static void VisitRecursive(INode node, State state)
        {
            if (node != null)
            {
                for (int i = 0; i < state.Visitors.Count; ++i)
                    state.Visitors[i].Enter(node, state.Context);

                node.Visit(_visitDelegate, state);

                for (int i = state.Visitors.Count - 1; i >= 0; --i)
                    state.Visitors[i].Leave(node, state.Context);
            }
        }

        private readonly struct State
        {
            public State(ValidationContext context, IList<INodeVisitor> visitors)
            {
                Context = context;
                Visitors = visitors;
            }

            public ValidationContext Context { get; }

            public IList<INodeVisitor> Visitors { get; }
        }
    }
}
