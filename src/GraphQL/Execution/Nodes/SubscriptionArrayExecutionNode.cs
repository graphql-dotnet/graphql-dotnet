using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <inheritdoc/>
    public class SubscriptionArrayExecutionNode : ArrayExecutionNode
    {
        /// <summary>
        /// Initializes an <see cref="SubscriptionArrayExecutionNode"/> instance with the specified values.
        /// </summary>
        public SubscriptionArrayExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode, object source)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
            Source = source;
        }

        /// <inheritdoc/>
        public override object Source { get; }
    }
}
