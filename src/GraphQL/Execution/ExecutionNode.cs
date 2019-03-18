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
        public string[] Path { get; protected set; }

        public string Name => Field?.Alias ?? Field?.Name;

        public bool IsResultSet { get; private set; }

        private object _result;
        public object Result
        {
            get { return _result; }
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

        protected ExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, string[] path)
        {
            Parent = parent;
            GraphType = graphType;
            Field = field;
            FieldDefinition = fieldDefinition;
            Path = path;
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
    }

    public interface IParentExecutionNode
    {
        IEnumerable<ExecutionNode> GetChildNodes();
    }

    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public IDictionary<string, ExecutionNode> SubFields { get; set; }

        public ObjectExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, string[] path)
            : base(parent, graphType, field, fieldDefinition, path)
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
            : base(null, graphType, null, null, new string[0])
        {

        }
    }

    public class ArrayExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public IList<ExecutionNode> Items { get; set; }

        public ArrayExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, string[] path)
            : base(parent, graphType, field, fieldDefinition, path)
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

                if (value == null && ((ListGraphType)item.FieldDefinition.ResolvedType).ResolvedType is NonNullGraphType)
                {
                    return null;
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
        public ValueExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, string[] path)
            : base(parent, graphType, field, fieldDefinition, path)
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
