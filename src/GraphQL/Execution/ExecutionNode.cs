using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public abstract class ExecutionNode : IResolveFieldContext<object>
    {
        public ExecutionContext Context { get; }
        public ExecutionNode Parent { get; }
        public IGraphType GraphType { get; }
        public Language.AST.Field Field { get; }
        public FieldType FieldDefinition { get; }
        public int? IndexInParentNode { get; protected set; }

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

        protected ExecutionNode(ExecutionContext context, ExecutionNode parent, IGraphType graphType, Language.AST.Field field, FieldType fieldDefinition, int? indexInParentNode)
        {
            Context = context;
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

        string IResolveFieldContext.FieldName => Field.Name;

        Language.AST.Field IResolveFieldContext.FieldAst => Field;

        FieldType IResolveFieldContext.FieldDefinition => FieldDefinition;

        IGraphType IResolveFieldContext.ReturnType => FieldDefinition.ResolvedType;

        IObjectGraphType IResolveFieldContext.ParentType => GetParentType(Context.Schema);

        private IDictionary<string, object> _arguments;
        IDictionary<string, object> IResolveFieldContext.Arguments => _arguments ?? (_arguments = ExecutionHelper.GetArgumentValues(Context.Schema, FieldDefinition.Arguments, Field.Arguments, Context.Variables));

        object IResolveFieldContext.RootValue => Context.RootValue;

        object IResolveFieldContext.Source => Source;

        ISchema IResolveFieldContext.Schema => Context.Schema;

        Document IResolveFieldContext.Document => Context.Document;

        Operation IResolveFieldContext.Operation => Context.Operation;

        Fragments IResolveFieldContext.Fragments => Context.Fragments;

        Variables IResolveFieldContext.Variables => Context.Variables;

        CancellationToken IResolveFieldContext.CancellationToken => Context.CancellationToken;

        Metrics IResolveFieldContext.Metrics => Context.Metrics;

        ExecutionErrors IResolveFieldContext.Errors => Context.Errors;

        private IDictionary<string, Language.AST.Field> _subFields;
        IDictionary<string, Language.AST.Field> IResolveFieldContext.SubFields => _subFields ?? (_subFields = ExecutionHelper.SubFieldsFor(Context, FieldDefinition.ResolvedType, Field));

        IDictionary<string, object> IProvideUserContext.UserContext => Context.UserContext;
    }

    public interface IParentExecutionNode
    {
        IEnumerable<ExecutionNode> GetChildNodes();
    }

    public class ObjectExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public IDictionary<string, ExecutionNode> SubFields { get; set; }

        public ObjectExecutionNode(ExecutionContext context, ExecutionNode parent, IGraphType graphType, Language.AST.Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(context, parent, graphType, field, fieldDefinition, indexInParentNode)
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
        public RootExecutionNode(ExecutionContext context, IObjectGraphType graphType)
            : base(context, null, graphType, null, null, null)
        {

        }
    }

    public class ArrayExecutionNode : ExecutionNode, IParentExecutionNode
    {
        public List<ExecutionNode> Items { get; set; }

        public ArrayExecutionNode(ExecutionContext context, ExecutionNode parent, IGraphType graphType, Language.AST.Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(context, parent, graphType, field, fieldDefinition, indexInParentNode)
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
        public ValueExecutionNode(ExecutionContext context, ExecutionNode parent, IGraphType graphType, Language.AST.Field field, FieldType fieldDefinition, int? indexInParentNode)
            : base(context, parent, graphType, field, fieldDefinition, indexInParentNode)
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
