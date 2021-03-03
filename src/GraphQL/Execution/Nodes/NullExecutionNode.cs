using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an execution node which always returns <see langword="null"/>.
    /// </summary>
    public class NullExecutionNode : ExecutionNode
    {
        /// <summary>
        /// Initializes an instance of <see cref="NullExecutionNode"/> with the specified values.
        /// </summary>
        public NullExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
            Result = null;
        }

        /// <summary>
        /// Returns <see langword="null"/>.
        /// </summary>
        public override object ToValue() => null;
    }
}
