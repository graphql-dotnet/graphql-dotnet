#nullable enable

using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <inheritdoc/>
    public class SubscriptionArrayExecutionNode : ArrayExecutionNode
    {
        /// <summary>
        /// Initializes an <see cref="SubscriptionArrayExecutionNode"/> instance with the specified values.
        /// </summary>
        public SubscriptionArrayExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode, object source)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
            Source = source;
        }

        /// <inheritdoc/>
        public override object Source { get; }
    }
}
