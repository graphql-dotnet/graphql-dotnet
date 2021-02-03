using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

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
        public List<ExecutionNode> Items { get; set; }

        /// <summary>
        /// Initializes an <see cref="ArrayExecutionNode"/> instance with the specified values.
        /// </summary>
        public ArrayExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> containing the results of the child execution nodes.
        /// </summary>
        public override object ToValue()
        {
            if (Items == null)
                return null;

            var items = new object[Items.Count];
            for (int i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];
                object value = item.ToValue();

                if (value == null)
                {
                    var listType = item.FieldDefinition.ResolvedType;
                    if (listType is NonNullGraphType nonNull)
                        listType = nonNull.ResolvedType;

                    if (((ListGraphType)listType).ResolvedType is NonNullGraphType)
                    {
                        return null;
                    }
                }

                items[i] = value;
            }

            return items;
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
