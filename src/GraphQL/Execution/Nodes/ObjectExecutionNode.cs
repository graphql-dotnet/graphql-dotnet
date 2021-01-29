using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an object execution node, which will contain child execution nodes.
    /// </summary>
    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode
    {
        /// <summary>
        /// Returns a dictionary of child execution nodes, with keys set to the names of the child fields that the child nodes represent.
        /// </summary>
        public Dictionary<string, ExecutionNode> SubFields { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="ObjectExecutionNode"/> with the specified values.
        /// </summary>
        public ObjectExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        /// <summary>
        /// For execution nodes that represent a field that is an <see cref="IAbstractGraphType"/>, returns the
        /// proper <see cref="IObjectGraphType"/> based on the set <see cref="ExecutionNode.Result"/>.
        /// Otherwise returns the value of <see cref="ExecutionNode.GraphType"/>.
        /// </summary>
        public IObjectGraphType GetObjectGraphType(ISchema schema)
        {
            var objectGraphType = GraphType as IObjectGraphType;

            if (GraphType is IAbstractGraphType abstractGraphType && IsResultSet)
                objectGraphType = abstractGraphType.GetObjectType(Result, schema);

            return objectGraphType;
        }

        /// <summary>
        /// Returns a representation of the result of this execution node and its children
        /// within a <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        public override object ToValue()
        {
            if (SubFields == null)
                return null;

            var fields = new ObjectProperty[SubFields.Count];

            int i = 0;
            foreach (var kvp in SubFields)
            {
                object value = kvp.Value.ToValue();

                if (value == null && kvp.Value.FieldDefinition.ResolvedType is NonNullGraphType)
                {
                    return null;
                }

                fields[i++] = new ObjectProperty(kvp.Key, value);
            }

            return fields;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes() => SubFields?.Values ?? Enumerable.Empty<ExecutionNode>();

        /// <inheritdoc/>
        public void ApplyToChildren<TState>(Action<ExecutionNode, TState> action, TState state, bool reverse = false)
        {
            if (SubFields != null)
            {
                if (reverse)
                {
                    foreach (var item in SubFields.Reverse()) //TODO: write custom enumerator for reverse
                        action(item.Value, state);
                }
                else
                {
                    foreach (var item in SubFields)
                        action(item.Value, state);
                }
            }
        }
    }
}
