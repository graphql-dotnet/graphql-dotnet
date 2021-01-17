using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.DataLoader;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents a node to be executed.
    /// </summary>
    public abstract class ExecutionNode
    {
        /// <summary>
        /// Returns the parent node, or null if this is the root node.
        /// </summary>
        public ExecutionNode Parent { get; }

        /// <summary>
        /// Returns the graph type of this node, unwrapped if it is a <see cref="NonNullGraphType"/>.
        /// Array nodes will be a <see cref="ListGraphType"/> instance.
        /// </summary>
        public IGraphType GraphType { get; }

        /// <summary>
        /// Returns the AST field of this node.
        /// </summary>
        public Field Field { get; }

        /// <summary>
        /// Returns the graph's field type of this node.
        /// </summary>
        public FieldType FieldDefinition { get; }

        /// <summary>
        /// For child array item nodes of a <see cref="ListGraphType"/>, returns the index of this array item within the field; otherwise, null.
        /// </summary>
        public int? IndexInParentNode { get; protected set; }

        /// <summary>
        /// Returns the underlying graph type of this node, retaining the <see cref="NonNullGraphType"/> wrapping if applicable.
        /// For child nodes of an array execution node, this property unwraps the <see cref="ListGraphType"/> instance and returns
        /// the underlying graph type, retaining the <see cref="NonNullGraphType"/> wrapping if applicable.
        /// </summary>
        internal IGraphType ResolvedType
        {
            get
            {
                if (IndexInParentNode.HasValue)
                {
                    return ((ListGraphType)Parent.GraphType).ResolvedType;
                }
                else
                {
                    return FieldDefinition?.ResolvedType;
                }
            }
        }

        /// <summary>
        /// Returns the AST field alias, if specified, or AST field name otherwise.
        /// </summary>
        public string Name => Field?.Alias ?? Field?.Name;

        /// <summary>
        /// Returns true if the result has been set. Also returns true when the result is temporarily set to an <see cref="IDataLoaderResult"/>
        /// pending execution at a later time.
        /// </summary>
        public bool IsResultSet { get; private set; }

        private object _result;
        /// <summary>
        /// Sets or returns the result of the execution node. May return a <see cref="IDataLoaderResult"/> if a node returns a data loader
        /// result that has not yet finished executing.
        /// </summary>
        public object Result
        {
            get => _result;
            set
            {
                IsResultSet = true;
                _result = value;
            }
        }

        private object _source;
        /// <summary>
        /// Returns the parent node's result. If set, the set value will override the parent node's result.
        /// </summary>
        public object Source
        {
            get => _source ?? Parent?.Result;
            set => _source = value;
        }

        /// <summary>
        /// Initializes an instance of <see cref="ExecutionNode"/> with the specified values
        /// </summary>
        /// <param name="parent">The parent node, or null if this is the root node</param>
        /// <param name="graphType">The graph type of this node, unwrapped if it is a <see cref="NonNullGraphType"/>. Array nodes will be a <see cref="ListGraphType"/> instance.</param>
        /// <param name="field">The AST field of this node</param>
        /// <param name="fieldDefinition">The graph's field type of this node</param>
        /// <param name="indexInParentNode">For child array item nodes of a <see cref="ListGraphType"/>, the index of this array item within the field; otherwise, null</param>
        protected ExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
        {
            Parent = parent;
            GraphType = graphType;
            Field = field;
            FieldDefinition = fieldDefinition;
            IndexInParentNode = indexInParentNode;
        }

        /// <summary>
        /// Returns an object that represents the result of this node.
        /// </summary>
        public abstract object ToValue();

        /// <summary>
        /// Returns the parent graph type of this node.
        /// </summary>
        public IObjectGraphType GetParentType(ISchema schema)
        {
            IGraphType parentType = Parent?.GraphType;

            if (parentType is IObjectGraphType objectType)
                return objectType;

            if (parentType is IAbstractGraphType abstractType && Parent.IsResultSet)
                return abstractType.GetObjectType(Parent.Result, schema);

            return null;
        }

        /// <summary>
        /// The path for the current node within the query.
        /// </summary>
        public IEnumerable<object> Path => GeneratePath(preferAlias: false);

        /// <summary>
        /// The path for the current node within the response.
        /// </summary>
        public IEnumerable<object> ResponsePath => GeneratePath(preferAlias: true);

        private static readonly object _num0 = 0;
        private static readonly object _num1 = 1;
        private static readonly object _num2 = 2;
        private static readonly object _num3 = 3;
        private static readonly object _num4 = 4;
        private static readonly object _num5 = 5;
        private static readonly object _num6 = 6;
        private static readonly object _num7 = 7;
        private static readonly object _num8 = 8;
        private static readonly object _num9 = 9;
        private static readonly object _num10 = 10;
        private static readonly object _num11 = 11;
        private static readonly object _num12 = 12;
        private static readonly object _num13 = 13;
        private static readonly object _num14 = 14;
        private static readonly object _num15 = 15;
        private static object GetObjectIndex(int index) => index switch
        {
            0 => _num0,
            1 => _num1,
            2 => _num2,
            3 => _num3,
            4 => _num4,
            5 => _num5,
            6 => _num6,
            7 => _num7,
            8 => _num8,
            9 => _num9,
            10 => _num10,
            11 => _num11,
            12 => _num12,
            13 => _num13,
            14 => _num14,
            15 => _num15,
            _ => index
        };

        private IEnumerable<object> GeneratePath(bool preferAlias)
        {
            var node = this;
            var count = 0;
            while (!(node is RootExecutionNode))
            {
                node = node.Parent;
                ++count;
            }

            if (count == 0)
                return Array.Empty<object>();

            var pathList = new object[count];
            var index = count;
            node = this;
            while (!(node is RootExecutionNode))
            {
                if (node.IndexInParentNode.HasValue)
                    pathList[--index] = GetObjectIndex(node.IndexInParentNode.Value);
                else
                    pathList[--index] = preferAlias ? node.Name : node.Field.Name;
                node = node.Parent;
            }

            return pathList;
        }
    }

    /// <summary>
    /// Represents an execution node with child nodes.
    /// </summary>
    public interface IParentExecutionNode
    {
        /// <summary>
        /// Returns a list of child execution nodes.
        /// </summary>
        IEnumerable<ExecutionNode> GetChildNodes();

        /// <summary>
        /// Applies the specified delegate to child execution nodes.
        /// </summary>
        /// <typeparam name="TState">Type of the provided state.</typeparam>
        /// <param name="action">Delegate to execute on every child node of this node.</param>
        /// <param name="state">An arbitrary state passed by the caller.</param>
        /// <param name="reverse">Specifies the direct or reverse direction of child nodes traversal.</param>
        void ApplyToChildren<TState>(Action<ExecutionNode, TState> action, TState state, bool reverse = false);
    }

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

            var fields = new Dictionary<string, object>(SubFields.Count);

            foreach (var kvp in SubFields)
            {
                var value = kvp.Value.ToValue();

                if (value == null && kvp.Value.FieldDefinition.ResolvedType is NonNullGraphType)
                {
                    return null;
                }

                fields[kvp.Key] = value;
            }

            return fields;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes()
        {
            return SubFields?.Values ?? Enumerable.Empty<ExecutionNode>();
        }

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

    /// <summary>
    /// Represents a root execution node.
    /// </summary>
    public class RootExecutionNode : ObjectExecutionNode
    {
        /// <summary>
        /// Initializes a new instance for the specified root graph type.
        /// </summary>
        public RootExecutionNode(IObjectGraphType graphType)
            : base(null, graphType, null, null, null)
        {

        }
    }

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

            var items = new List<object>(Items.Count);
            foreach (ExecutionNode item in Items)
            {
                var value = item.ToValue();

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

                items.Add(value);
            }

            return items;
        }

        IEnumerable<ExecutionNode> IParentExecutionNode.GetChildNodes()
        {
            return Items ?? Enumerable.Empty<ExecutionNode>();
        }

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
        public override object ToValue()
        {
            return Result;
        }

        /// <inheritdoc cref="ExecutionNode.GraphType"/>
        public new ScalarGraphType GraphType => (ScalarGraphType)base.GraphType;
    }

    /// <summary>
    /// Represents an execution node which always returns null.
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
        /// Returns null.
        /// </summary>
        public override object ToValue() => null;
    }
}
