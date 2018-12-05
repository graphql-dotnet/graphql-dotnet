using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public class ExecutionNodeDefinition
    {
        public IGraphType GraphType { get; }
        public Field Field { get; }
        public FieldType FieldDefinition { get; }

        public string Name => Field?.Alias ?? Field?.Name;

        public ExecutionNodeDefinition(IGraphType graphType, Field field, FieldType fieldDefinition)
        {
            GraphType = graphType;
            Field = field;
            FieldDefinition = fieldDefinition;
        }
    }

    public abstract class ExecutionNode
    {
        private readonly ExecutionNodeDefinition _definition;
        public IGraphType GraphType => _definition.GraphType;
        public Field Field => _definition.Field;
        public FieldType FieldDefinition => _definition.FieldDefinition;
        public string Name => _definition.Name;

        public ExecutionNode Parent { get; }

        public string[] Path { get; protected set; }

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
            :this(new ExecutionNodeDefinition(graphType, field, fieldDefinition), parent, path)
        { }

        protected ExecutionNode(ExecutionNodeDefinition definition, ExecutionNode parent, string[] path)
        {
            _definition = definition;
            Parent = parent;
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
        { }

        public ObjectExecutionNode(ExecutionNodeDefinition definition, ExecutionNode parent, string[] path)
            : base(definition, parent, path)
        { }

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
                fields[kvp.Key] = kvp.Value.ToValue();
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
        { }

        public ArrayExecutionNode(ExecutionNodeDefinition definition, ExecutionNode parent, string[] path)
            : base(definition, parent, path)
        { }

        public override object ToValue()
        {
            if (Items == null)
                return null;

            return Items
                .Select(x => x.ToValue())
                .ToList();
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
        { }

        public ValueExecutionNode(ExecutionNodeDefinition definition, ExecutionNode parent, string[] path)
            : base(definition, parent, path)
        { }

        public override object ToValue()
        {
            if (Result == null)
                return null;

            var scalarType = GraphType as ScalarGraphType;
            return scalarType?.Serialize(Result);
        }
    }
}
