using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents a execution node of a <see cref="ScalarGraphType"/>.
    /// </summary>
    public class ValueExecutionNode : ExecutionNode
    {
        /// <summary>
        /// Initializes an instance of <see cref="ValueExecutionNode"/> with the specified values.
        /// </summary>
        public ValueExecutionNode(ExecutionNode parent, ScalarGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        /// <summary>
        /// Returns <see cref="ExecutionNode.Result"/>, which has already been serialized by <see cref="ScalarGraphType.Serialize(object)"/>
        /// within <see cref="ExecutionStrategy.CompleteNode(ExecutionContext, ExecutionNode)"/> or
        /// <see cref="ExecutionStrategy.SetArrayItemNodes(ExecutionContext, ArrayExecutionNode)"/>.
        /// </summary>
        public override object? ToValue() => Result;

        /// <inheritdoc cref="ExecutionNode.GraphType"/>
        public new ScalarGraphType GraphType => (ScalarGraphType)base.GraphType!;
    }
}
