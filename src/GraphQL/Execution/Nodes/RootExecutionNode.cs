using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents a root execution node.
    /// </summary>
    public class RootExecutionNode : ObjectExecutionNode
    {
        /// <summary>
        /// Initializes a new instance for the specified root graph type.
        /// </summary>
        public RootExecutionNode(IObjectGraphType graphType, GraphQLSelectionSet? selectionSet)
            : base(null!, graphType, null!, null!, null)
        {
            SelectionSet = selectionSet;
        }

        /// <inheritdoc/>
        public override GraphQLSelectionSet? SelectionSet { get; }
    }
}
