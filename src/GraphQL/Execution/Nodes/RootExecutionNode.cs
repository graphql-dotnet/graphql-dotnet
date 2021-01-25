using GraphQL.Types;

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
        public RootExecutionNode(IObjectGraphType graphType)
            : base(null, graphType, null, null, null)
        {
        }
    }
}
