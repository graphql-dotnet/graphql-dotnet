using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public abstract class ExecutionNode
    {
        public ExecutionNode Parent { get; }
        public IGraphType GraphType { get; }
        public Field Field { get; }
        public FieldType FieldDefinition { get; }
        public int? IndexInParentNode { get; protected set; }

        public string Name => Field?.Alias ?? Field?.Name;

        public bool IsResultSet { get; private set; }

        private object _result;
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
        public object Source
        {
            get => _source ?? Parent?.Result;
            set => _source = value;
        }

        protected ExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
        {
            Parent = parent;
            GraphType = graphType;
            Field = field;
            FieldDefinition = fieldDefinition;
            IndexInParentNode = indexInParentNode;
        }

        public abstract object ToValue();

        public IObjectGraphType GetParentType(ISchema schema)
        {
            IGraphType parentType = Parent?.GraphType;

            if (parentType is IObjectGraphType objectType)
                return objectType;

            if (parentType is IAbstractGraphType abstractType && Parent.IsResultSet)
                return abstractType.GetObjectType(Parent.Result, schema);

            return null;
        }

        public IEnumerable<object> Path
        {
            get
            {
                var node = this;
                var count = 0;
                while (!(node is RootExecutionNode))
                {
                    node = node.Parent;
                    ++count;
                }

                var pathList = new object[count];
                var index = count;
                node = this;
                while (!(node is RootExecutionNode))
                {
                    if (node.IndexInParentNode.HasValue)
                        pathList[--index] = GetObjectIndex(node.IndexInParentNode.Value);
                    else
                        pathList[--index] = node.Field.Name;
                    node = node.Parent;
                }

                return pathList;
            }
        }

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
    }

    public interface IParentExecutionNode
    {
        IEnumerable<ExecutionNode> GetChildNodes();
    }

    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public IDictionary<string, ExecutionNode> SubFields { get; set; }

        public ObjectExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {
        }

        public IObjectGraphType GetObjectGraphType(ISchema schema)
        {
            var objectGraphType = GraphType as IObjectGraphType;

            if (GraphType is IAbstractGraphType abstractGraphType && IsResultSet)
                objectGraphType = abstractGraphType.GetObjectType(Result, schema);

            return objectGraphType;
        }

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
    }

    public class RootExecutionNode : ObjectExecutionNode
    {
        public RootExecutionNode(IObjectGraphType graphType)
            : base(null, graphType, null, null, null)
        {

        }
    }

    public class ArrayExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public List<ExecutionNode> Items { get; set; }

        public ArrayExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {

        }

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
    }

    public class ValueExecutionNode : ExecutionNode
    {
        public ValueExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(parent, graphType, field, fieldDefinition, indexInParentNode)
        {

        }

        public override object ToValue()
        {
            // result has already been serialized within ExecuteNodeAsync / SetArrayItemNodes
            return Result;
        }
    }
}
