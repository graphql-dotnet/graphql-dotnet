#nullable enable

using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <inheritdoc/>
    public class SubscriptionValueExecutionNode : ValueExecutionNode
    {
        /// <summary>
        /// Initializes an <see cref="SubscriptionValueExecutionNode"/> instance with the specified values.
        /// </summary>
        public SubscriptionValueExecutionNode(ExecutionNode parent, ScalarGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode, object source)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
            Source = source;
        }

        /// <inheritdoc/>
        public override object Source { get; }
    }
}
