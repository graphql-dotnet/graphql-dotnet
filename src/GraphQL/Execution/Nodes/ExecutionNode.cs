using System.Diagnostics;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents a node to be executed.
    /// </summary>
    public abstract class ExecutionNode
    {
        /// <summary>
        /// Returns the parent node, or <see langword="null"/> if this is the root node.
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
        public GraphQLField Field { get; }

        /// <summary>
        /// Returns the graph's field type of this node.
        /// </summary>
        public FieldType FieldDefinition { get; }

        /// <summary>
        /// For child array item nodes of a <see cref="ListGraphType"/>, returns the index of this array item within the field; otherwise, <see langword="null"/>.
        /// </summary>
        public int? IndexInParentNode { get; }

        /// <summary>
        /// Returns the underlying graph type of this node, retaining the <see cref="NonNullGraphType"/> wrapping if applicable.
        /// For child nodes of an array execution node, this property unwraps the <see cref="ListGraphType"/> instance and returns
        /// the underlying graph type, retaining the <see cref="NonNullGraphType"/> wrapping if applicable.
        /// </summary>
        internal IGraphType? ResolvedType
        {
            get
            {
                return IndexInParentNode.HasValue
                    ? ((ListGraphType?)Parent!.GraphType)?.ResolvedType
                    : FieldDefinition?.ResolvedType;
            }
        }

        /// <summary>
        /// Returns the AST field alias, if specified, or AST field name otherwise.
        /// </summary>
        public string? Name => Field?.Alias != null ? Field.Alias.Name.StringValue : FieldDefinition?.Name; //ISSUE:allocation in case of alias

        /// <summary>
        /// Sets or returns the result of the execution node. May return a <see cref="IDataLoaderResult"/> if a node returns a data loader
        /// result that has not yet finished executing.
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Returns the parent node's result.
        /// </summary>
        public virtual object? Source => Parent?.Result;

        /// <summary>
        /// Initializes an instance of <see cref="ExecutionNode"/> with the specified values
        /// </summary>
        /// <param name="parent">The parent node, or <see langword="null"/> if this is the root node</param>
        /// <param name="graphType">The graph type of this node, unwrapped if it is a <see cref="NonNullGraphType"/>. Array nodes will be a <see cref="ListGraphType"/> instance.</param>
        /// <param name="field">The AST field of this node</param>
        /// <param name="fieldDefinition">The graph's field type of this node</param>
        /// <param name="indexInParentNode">For child array item nodes of a <see cref="ListGraphType"/>, the index of this array item within the field; otherwise, <see langword="null"/></param>
        protected ExecutionNode(ExecutionNode parent, IGraphType graphType, GraphQLField field, FieldType fieldDefinition, int? indexInParentNode)
        {
            Debug.Assert(field?.Name == fieldDefinition?.Name); // ? for RootExecutionNode

            Parent = parent;
            GraphType = graphType;
            Field = field!;
            FieldDefinition = fieldDefinition!;
            IndexInParentNode = indexInParentNode;
        }

        /// <summary>
        /// Returns an object that represents the result of this node.
        /// </summary>
        public abstract object? ToValue();

        /// <summary>
        /// Prepares this node and children nodes for serialization. Returns <see langword="true"/> if this node should return <see langword="null"/>.
        /// </summary>
        public virtual bool PropagateNull() => ToValue() == null;

        /// <summary>
        /// Returns the parent graph type of this node.
        /// </summary>
        public IObjectGraphType? GetParentType(ISchema schema)
        {
            IGraphType? parentType = Parent?.GraphType;

            if (parentType is IObjectGraphType objectType)
                return objectType;

            if (parentType is IAbstractGraphType abstractType)
                return abstractType.GetObjectType(Parent!.Result!, schema);

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
            while (node is not RootExecutionNode)
            {
                node = node.Parent!;
                ++count;
            }

            if (count == 0)
                return Array.Empty<object>();

            var pathList = new object[count];
            var index = count;
            node = this;
            while (node is not RootExecutionNode)
            {
                pathList[--index] = node.IndexInParentNode.HasValue
                    ? GetObjectIndex(node.IndexInParentNode.Value)
                    : preferAlias ? node.Name! : node.FieldDefinition.Name;
                node = node.Parent!;
            }

            return pathList;
        }
    }
}
