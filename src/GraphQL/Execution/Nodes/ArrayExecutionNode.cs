using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an execution node of a <see cref="ListGraphType"/>.
    /// </summary>
    public class ArrayExecutionNode : ExecutionNode, IParentExecutionNode, IReadOnlyCollection<object>
    {
        /// <summary>
        /// Returns a list of child execution nodes.
        /// </summary>
        public List<ExecutionNode> Items { get; set; }

        int IReadOnlyCollection<object>.Count => Items.Count;

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

        public override bool ClearErrorNodes()
        {
            if (Items == null)
                return true;

            if (Items.Count == 0)
                return false;

            var ok = false;

            for (int i = 0; i < Items.Count; ++i)
            {
                var item = Items[i];
                bool valueIsNull = item.ClearErrorNodes();

                if (valueIsNull && !ok)
                {
                    if (((ListGraphType)GraphType).ResolvedType is NonNullGraphType)
                    {
                        Items = null;
                        return true;
                    }
                    else
                    {
                        ok = true;
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

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        private IEnumerator<object> GetEnumerator()
        {
            foreach (var node in Items)
            {
                yield return node switch
                {
                    ArrayExecutionNode arrayNode => arrayNode.Items == null ? null : arrayNode,
                    ObjectExecutionNode objectNode => objectNode.SubFields == null ? null : objectNode,
                    null => null,
                    _ => node.ToValue()
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
