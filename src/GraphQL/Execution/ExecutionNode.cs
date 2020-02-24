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

        public IEnumerable<string> Path
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

                var pathList = new string[count];
                var index = count;
                node = this;
                while (!(node is RootExecutionNode))
                {
                    if (node.IndexInParentNode.HasValue)
                        pathList[--index] = GetStringIndex(node.IndexInParentNode.Value);
                    else
                        pathList[--index] = node.Field.Name;
                    node = node.Parent;
                }

                return pathList;
            }
        }

        private static string GetStringIndex(int index) => index switch
        {
            0 => "0",
            1 => "1",
            2 => "2",
            3 => "3",
            4 => "4",
            5 => "5",
            6 => "6",
            7 => "7",
            8 => "8",
            9 => "9",
            10 => "10",
            11 => "11",
            12 => "12",
            13 => "13",
            14 => "14",
            15 => "15",
            _ => index.ToString()
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
            if (Result == null)
                return null;

            var scalarType = GraphType as ScalarGraphType;
            return scalarType?.Serialize(Result);
        }
    }
}
