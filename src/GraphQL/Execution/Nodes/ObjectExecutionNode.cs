using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an object execution node, which will contain child execution nodes.
    /// </summary>
    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode
    {
        /// <summary>
        /// Returns an array of child execution nodes.
        /// </summary>
        public ExecutionNode[]? SubFields { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="ObjectExecutionNode"/> with the specified values.
        /// </summary>
        public ObjectExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        /// <summary>
        /// For execution nodes that represent a field that is an <see cref="IAbstractGraphType"/>, returns the
        /// proper <see cref="IObjectGraphType"/> based on the set <see cref="ExecutionNode.Result"/>.
        /// Otherwise returns the value of <see cref="ExecutionNode.GraphType"/>.
        /// </summary>
        public IObjectGraphType? GetObjectGraphType(ISchema schema)
        {
            var objectGraphType = GraphType as IObjectGraphType;

            if (GraphType is IAbstractGraphType abstractGraphType)
                objectGraphType = abstractGraphType.GetObjectType(Result!, schema);

            return objectGraphType;
        }

        /// <summary>
        /// Returns a representation of the result of this execution node and its children
        /// within a <see cref="Dictionary{TKey, TValue}"/>.
        /// <see cref="PropagateNull"/> must be called prior to calling this method.
        /// </summary>
        public override object? ToValue()
        {
            if (SubFields == null)
                return null;

            var fields = new Dictionary<string, object?>(SubFields.Length);

            for (int i = 0; i < SubFields.Length; ++i)
            {
                var child = SubFields[i];
                fields.Add(child.Name!, child.ToValue());
            }

            return fields;
        }

        /// <inheritdoc/>
        public override bool PropagateNull()
        {
            if (SubFields == null)
                return true;

            for (int i = 0; i < SubFields.Length; ++i)
            {
                var child = SubFields[i];
                bool valueIsNull = child.PropagateNull();

                if (valueIsNull && child.FieldDefinition!.ResolvedType is NonNullGraphType)
                {
                    SubFields = null;
                    return true;
                }
            }

            return false;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes() => SubFields ?? Enumerable.Empty<ExecutionNode>();

        /// <summary>
        /// Returns the selection set from <see cref="ExecutionNode.Field"/>.
        /// </summary>
        public virtual GraphQLSelectionSet? SelectionSet => Field?.SelectionSet;

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
    }
}
