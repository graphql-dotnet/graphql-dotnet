using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an object execution node, which will contain child execution nodes.
    /// </summary>
    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode, IReadOnlyDictionary<string, object>
    {
        /// <summary>
        /// Returns an array of child execution nodes.
        /// </summary>
        public ExecutionNode[] SubFields { get; set; }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => SubFields.Length;

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => throw new NotImplementedException();

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => throw new NotImplementedException();

        object IReadOnlyDictionary<string, object>.this[string key] => throw new NotImplementedException();

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

            if (GraphType is IAbstractGraphType abstractGraphType)
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

            var fields = new ObjectProperty[SubFields.Length];

            for (int i = 0; i < SubFields.Length; ++i)
            {
                var child = SubFields[i];
                object value = child.ToValue();

                if (value == null && child.FieldDefinition.ResolvedType is NonNullGraphType)
                {
                    return null;
                }

                fields[i] = new ObjectProperty(child.Name, value);
            }

            return fields;
        }

        public override bool ClearErrorNodes()
        {
            if (SubFields == null)
                return true;

            for (int i = 0; i < SubFields.Length; ++i)
            {
                var child = SubFields[i];
                bool valueIsNull = child.ClearErrorNodes();

                if (valueIsNull)
                {
                    if (child.FieldDefinition.ResolvedType is NonNullGraphType)
                    {
                        SubFields = null;
                        return true;
                    }
                }
            }

            return false;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes() => SubFields ?? Enumerable.Empty<ExecutionNode>();

        /// <inheritdoc/>
        public void ApplyToChildren<TState>(Action<ExecutionNode, TState> action, TState state, bool reverse = false)
        {
            if (SubFields != null)
            {
                if (reverse)
                {
                    for (int i = SubFields.Length - 1; i >= 0; --i)
                        action(SubFields[i], state);
                }
                else
                {
                    foreach (var item in SubFields)
                        action(item, state);
                }
            }
        }

        private IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var node in SubFields)
            {
                var result = node switch
                {
                    ArrayExecutionNode arrayNode => arrayNode.Items == null ? null : arrayNode,
                    ObjectExecutionNode objectNode => objectNode.SubFields == null ? null : objectNode,
                    _ => node.ToValue()
                };
                yield return new KeyValuePair<string, object>(node?.Name, result);
            }
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => throw new NotImplementedException();
        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => throw new NotImplementedException();
    }
}
