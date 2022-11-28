using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <inheritdoc/>
    public class SubscriptionObjectExecutionNode : ObjectExecutionNode
    {
        /// <summary>
        /// Initializes an instance of <see cref="SubscriptionObjectExecutionNode"/> with the specified values.
        /// </summary>
        public SubscriptionObjectExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode, object source)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
            Source = source;
        }

        /// <inheritdoc/>
        public override object Source { get; }
    }
}
