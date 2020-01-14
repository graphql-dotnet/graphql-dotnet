using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Execution
{
    public class ReadonlyResolveFieldContext : IResolveFieldContext, IResolveFieldContext<object>
    {
        private readonly ExecutionNode _executionNode;
        private readonly ExecutionContext _executionContext;
        private Dictionary<string, object> _arguments;
        private IDictionary<string, Field> _subFields;

        public ReadonlyResolveFieldContext(ExecutionNode node, ExecutionContext context)
        {
            _executionNode = node ?? throw new ArgumentNullException(nameof(node));
            _executionContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        private IDictionary<string, Field> GetSubFields()
        {
            return ExecutionHelper.SubFieldsFor(_executionContext, _executionNode.FieldDefinition.ResolvedType, _executionNode.Field);
        }

        private Dictionary<string, object> GetArguments()
        {
            return ExecutionHelper.GetArgumentValues(_executionContext.Schema, _executionNode.FieldDefinition.Arguments, _executionNode.Field.Arguments, _executionContext.Variables);
        }

        public object Source => _executionNode.Source;

        public string FieldName => _executionNode.Field.Name;

        public Language.AST.Field FieldAst => _executionNode.Field;

        public FieldType FieldDefinition => _executionNode.FieldDefinition;

        public IGraphType ReturnType => _executionNode.FieldDefinition.ResolvedType;

        public IObjectGraphType ParentType => _executionNode.GetParentType(_executionContext.Schema);

        public Dictionary<string, object> Arguments => _arguments ?? (_arguments = GetArguments());

        public object RootValue => _executionContext.RootValue;

        public ISchema Schema => _executionContext.Schema;

        public Document Document => _executionContext.Document;

        public Operation Operation => _executionContext.Operation;

        public Fragments Fragments => _executionContext.Fragments;

        public Variables Variables => _executionContext.Variables;

        public CancellationToken CancellationToken => _executionContext.CancellationToken;

        public Metrics Metrics => _executionContext.Metrics;

        public ExecutionErrors Errors => _executionContext.Errors;

        public IEnumerable<string> Path => _executionNode.Path;

        public IDictionary<string, Language.AST.Field> SubFields => _subFields ?? (_subFields = GetSubFields());

        public IDictionary<string, object> UserContext => _executionContext.UserContext;

        object IResolveFieldContext.Source => _executionNode.Source;
    }
}
