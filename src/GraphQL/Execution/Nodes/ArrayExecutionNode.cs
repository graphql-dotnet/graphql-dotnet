using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an execution node of a <see cref="ListGraphType"/>.
    /// </summary>
    public class ArrayExecutionNode : ExecutionNode, IParentExecutionNode
    {
        /// <summary>
        /// Returns a list of child execution nodes.
        /// </summary>
        public List<ExecutionNode>? Items { get; set; }

        /// <summary>
        /// Initializes an <see cref="ArrayExecutionNode"/> instance with the specified values.
        /// </summary>
        public ArrayExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        /// <summary>
        /// Returns an object array containing the results of the child execution nodes.
        /// <see cref="PropagateNull"/> must be called prior to calling this method.
        /// </summary>
        public override object? ToValue()
        {
            if (Items == null)
                return null;

            var items = new object?[Items.Count];
            for (int i = 0; i < Items.Count; ++i)
            {
                items[i] = Items[i].ToValue();
            }

            return items;
        }

        /// <inheritdoc/>
        public override bool PropagateNull()
        {
            if (Items == null)
                return true;

            if (Items.Count == 0)
                return false;

            var isNullableType = false;

            for (int i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];
                bool valueIsNull = item.PropagateNull();

                if (valueIsNull && !isNullableType)
                {
                    if (((ListGraphType)GraphType!).ResolvedType is NonNullGraphType)
                    {
                        Items = null;
                        return true;
                    }
                    else
                    {
                        isNullableType = true;
                    }
                }
            }

            return false;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes() => Items ?? Enumerable.Empty<ExecutionNode>();

        /// <inheritdoc/>
        public void ApplyToChildren<TState>(Action<ExecutionNode, TState> action, TState state, bool reverse = false)
        {
            if (Items != null)
            {
                if (reverse)
                {
                    for (int i = Items.Count - 1; i >= 0; --i)
                        action(Items[i], state);
                }
                else
                {
                    foreach (var item in Items)
                        action(item, state);
                }
            }
        }
    }
}
